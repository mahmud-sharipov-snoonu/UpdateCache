# UpdateCache Sample

Sample project for caching homepage "updates" (coins earned, quest completed, badge unlocked) in Redis
instead of recalculating them on every homepage open.

Events arrive over RabbitMQ (MassTransit), one consumer per event type maps them into a shared
`UpdateModel`, and the customer-facing endpoint drains them from Redis — reading removes them.

## Flow

```
publisher --(RabbitMQ)--> CoinsEarnedConsumer        --\
                          QuestCompletedConsumer      --> RPUSH updates:{userId}  (Redis list)
                          AchievementUnlockedConsumer --/
                                                              |
                                  GET /users/{userId}/updates  |  atomic drain (Lua: LRANGE + DEL)
                                                              v
                                                       oldest-first batch, key emptied
```

## Atomicity

`LRANGE` + `DEL` as two separate calls would have a race: a consumer's `RPUSH` could land between
them and be deleted without ever being returned. `RedisUpdateStore.DrainAsync` therefore runs both
inside a single Lua script — Redis executes a script as one indivisible unit, so a concurrent
`RPUSH` is either fully applied before the script runs, or queued until after the `DEL`. Either way
the update is returned exactly once.

Verified under load: 2000 concurrent writes against two parallel readers draining the same key
returned 2000 unique updates, 0 duplicates, 0 missing.

## Running

```bash
docker compose up -d
dotnet run --project src/UpdateCache.Sample
```

Swagger UI: http://localhost:5174/swagger — both endpoints, plus the `UpdateModel` schema.

Or from the shell — publish some events and drain them:

```bash
U=11111111-1111-1111-1111-111111111111
curl -X POST http://localhost:5174/demo/publish/coins -H 'Content-Type: application/json' -d "{\"userId\":\"$U\"}"
curl -X POST http://localhost:5174/demo/publish/quest -H 'Content-Type: application/json' -d "{\"userId\":\"$U\"}"
curl http://localhost:5174/users/$U/updates   # returns both, empties the key
curl http://localhost:5174/users/$U/updates   # []
```

RabbitMQ management UI: http://localhost:15672 (guest/guest).

## Layout

| Path | Purpose |
| --- | --- |
| `Contracts/Events.cs` | The three inbound event contracts |
| `Updates/UpdateModel.cs` | Shared cached model (`Source`, `Type`, `Title`, `Subtitle`, `OccurredAtUtc`) |
| `Updates/RedisUpdateStore.cs` | `RPUSH` write + atomic Lua drain |
| `Consumers/` | One MassTransit consumer per event, each mapping to `UpdateModel` |
| `Program.cs` | MassTransit/RabbitMQ/Redis wiring, endpoints |

## Sample-only simplifications

- `POST /demo/publish/{eventType}` exists only to trigger the pipeline; a real system has upstream publishers.
- `UpdateSource.Order` is defined but unused — no order event in this sample.
- `Title`/`Subtitle` are pre-rendered strings; production would likely carry a structured payload and localize at render time.
- No auth, no validation, no per-user TTL on the Redis keys.
- MassTransit pinned to 8.x (Apache-2.0). v9 requires a commercial license.
