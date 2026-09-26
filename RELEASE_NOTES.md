# Release notes

## 4.0.0

### ⚠️ Breaking changes

- Dropped .NET 8 support, .NET 10 with EF Core 10 is required.
- The outbox table requires a migration. Add an EF Core migration and apply it **before** deploying this version. Older versions keep working with the new schema, so a rolling deployment is safe.
  - New columns: `AttemptCount`, `NextAttemptAtUtc`, `ParkedAtUtc` and `LastError`.
  - New filtered index on pending messages.
  - Increased max length of `MessageType` (500) and `DestinationTopicName` (260).

### Features

- Failed send attempts are retried with exponential backoff (`RetryBackoffBase`, `RetryBackoffMax`), so a message that keeps failing no longer blocks the outbox.
- Optionally park messages after `MaxSendAttempts` failed send attempts. By default, messages are never parked.

## 3.2.0

### Features

- Messages with a type that isn't handled by the receiving handler are dead-lettered immediately with reason `Unsupported message type`.
- Using the same `DbContext` for the outbox of multiple buses now throws, as each bus requires its own `DbContext`.

### Fixes

- Messages published through a base type or interface are routed and serialized by their runtime type.
- Thread-safe listener registration in the in-memory transport.
- Processor error logs show the queue name instead of the namespace.
