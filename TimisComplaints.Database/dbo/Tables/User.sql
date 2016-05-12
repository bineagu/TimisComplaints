﻿CREATE TABLE [dbo].[User] (
    [Id]       UNIQUEIDENTIFIER NOT NULL,
    [Email]    NVARCHAR (100)   NOT NULL,
    [Password] NVARCHAR (50)    NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id] ASC)
);

