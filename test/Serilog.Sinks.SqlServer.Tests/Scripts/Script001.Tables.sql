IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogEvent]') AND type in (N'U'))
CREATE TABLE [dbo].[LogEvent]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Timestamp] DATETIMEOFFSET NOT NULL,
    [Level] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [TraceId] NVARCHAR(100) NULL,
    [SpanId] NVARCHAR(100) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [SourceContext] NVARCHAR(1000) NULL,
    CONSTRAINT [PK_LogEvent] PRIMARY KEY CLUSTERED ([Id] ASC),
    INDEX [IX_LogEvent_TimeStamp] NONCLUSTERED ([Timestamp] DESC),
    INDEX [IX_LogEvent_Level] NONCLUSTERED ([Level] ASC),
    INDEX [IX_LogEvent_TraceId] NONCLUSTERED ([TraceId] ASC),
)
WITH
(
    DATA_COMPRESSION = PAGE
);

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogExtended]') AND type in (N'U'))
CREATE TABLE [dbo].[LogExtended]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Timestamp] DATETIMEOFFSET NOT NULL,
    [Level] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [TraceId] NVARCHAR(100) NULL,
    [SpanId] NVARCHAR(100) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [SourceContext] NVARCHAR(1000) NULL,
    [ApplicationName] NVARCHAR(500) NULL,
    [ApplicationVersion] NVARCHAR(500) NULL,
    [EnvironmentName] NVARCHAR(500) NULL,
    CONSTRAINT [PK_LogExtended] PRIMARY KEY CLUSTERED ([Id] ASC),
    INDEX [IX_LogExtended_TimeStamp] NONCLUSTERED ([Timestamp] DESC),
    INDEX [IX_LogExtended_Level] NONCLUSTERED ([Level] ASC),
    INDEX [IX_LogExtended_TraceId] NONCLUSTERED ([TraceId] ASC),
    INDEX [IX_LogExtended_SpanId] NONCLUSTERED ([SpanId] ASC),
)
WITH
(
    DATA_COMPRESSION = PAGE
);
