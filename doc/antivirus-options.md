# Antivirus Scanning Options

## ClamAV

```mermaid
sequenceDiagram
    actor User
    participant API as ASP.NET Core API<br/>(ECS Fargate)
    participant ClamAV as ClamAV<br/>(ECS Sidecar)
    participant S3 as S3 Bucket/Db

    User->>API: POST /api/files/upload (file)
    API->>ClamAV: Stream file (nClam)
    ClamAV-->>API: Clean / Virus Detected / Error
    alt Clean
        API->>S3: Upload file
        API-->>User: 200 OK
    else Infected
        API-->>User: 400 Bad Request
    end
```

## AWS Guard Duty

```mermaid
sequenceDiagram
    actor User
    participant API as ASP.NET Core API<br/>(ECS Fargate)
    participant S3 as S3 Quarantine Bucket
    participant GD as GuardDuty<br/>Malware Protection
    participant SNS as SNS Topic
    participant SQS as SQS Queue
    participant HF as Hangfire Worker<br/>(ECS)

    User->>API: POST /api/files/upload (file)
    API->>S3: PutObject (quarantine prefix)
    API-->>User: 202 Accepted (uploadId)

    S3-->>GD: Triggers on PutObject (automatic)
    GD->>S3: Tags object (NO_THREATS_FOUND / THREATS_FOUND)
    GD->>SNS: Publish scan result event
    SNS->>SQS: Forward message

    HF->>SQS: Poll for results
    HF->>API: Update upload record status in DB

    User->>API: GET /api/files/upload/{id}/status
    API-->>User: pending / clean / infected
```

## Questions

1. Would there be a chance moving forward that judges will upload sensitive documents that should only live in Emerald?
2. Is there a limit of the file size the judge will upload?
3. How does the upload experience would like look in the Frontend?
4. GuardDuty appears to cost more.
