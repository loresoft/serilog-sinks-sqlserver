CREATE TABLE [LogsMSSqlServerBatch] (

   [Id] int IDENTITY(1,1) NOT NULL,
   [Message] nvarchar(max) NULL,
   [MessageTemplate] nvarchar(max) NULL,
   [Level] nvarchar(max) NULL,
   [TimeStamp] datetime NULL,
   [Exception] nvarchar(max) NULL,
   [Properties] nvarchar(max) NULL

   CONSTRAINT [PK_LogsMSSqlServerBatch] PRIMARY KEY CLUSTERED ([Id] ASC)
);
