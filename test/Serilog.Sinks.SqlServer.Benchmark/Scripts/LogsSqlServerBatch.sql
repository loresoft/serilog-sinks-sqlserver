CREATE TABLE [dbo].[LogsSqlServerBatch]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Timestamp] DATETIMEOFFSET NOT NULL,
    [Level] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [TraceId] NVARCHAR(100) NULL,
    [SpanId] NVARCHAR(100) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_LogsSqlServerBatch] PRIMARY KEY CLUSTERED ([Id] ASC)
);
