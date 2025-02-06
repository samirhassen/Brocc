using NDesk.Options;

namespace NTechDbTool
{
    public static class AzureAdUserScriptCreator
    {
        public static int CreateScript(string[] args)
        {
            string? userObjectId = null;
            string? displayName = null;

            var p = new OptionSet()
                {
                    { "object-id=", "azure user object id (a guid)", v => userObjectId = v },
                    { "display-name=", "user display name", v => displayName = v },
                };

            p.Parse(args);

            if (string.IsNullOrWhiteSpace(userObjectId) || string.IsNullOrWhiteSpace(displayName))
            {
                CommandLineUtils.ShowHelp(p, "create-azure-user-script");
                return 0;
            }

            var script = GenerateScript(displayName, userObjectId);
            Console.WriteLine("--Script--");
            Console.WriteLine(script);

            return 0;
        }

        private static string GenerateScript(string userDisplayName, string azureObjectId)
        {
            const string ScriptPattern =
            @"declare @userDisplayName nvarchar(100) = '[[USER_DISPLAY_NAME]]'
declare @userIdentity nvarchar(128) = '[[USER_IDENTITY]]'
declare @initialDate datetime = GETDATE()
declare @createdByUserId int = isnull((select min(Id) from [User]), 0)
declare @userId int

--User
insert into [User] (CreationDate, CreatedById, DisplayName, IsSystemUser) values (@initialDate, @createdByUserId, @userDisplayName, 0)
select @userId = scope_identity()

--Login
INSERT INTO [AuthenticationMechanism]
           ([CreationDate]
           ,[CreatedById]
           ,[UserIdentity]
           ,[AuthenticationType]
           ,[AuthenticationProvider]
           ,[UserId])
     VALUES
           (@initialDate
           ,@createdByUserId
           ,@userIdentity
           ,'OpenIdConnect'
           ,'AzureAd'
           ,@userId)

--Groups
declare @ProductName nvarchar(50) = 'ConsumerCreditFi'

DECLARE @GroupNames TABLE (Value NVARCHAR(50))
INSERT INTO @GroupNames VALUES ('Admin')
INSERT INTO @GroupNames VALUES ('High')
INSERT INTO @GroupNames VALUES ('Low')
INSERT INTO @GroupNames VALUES ('Middle')

DECLARE @GroupName VARCHAR(50)
DECLARE db_cursor CURSOR FOR  
SELECT Value FROM @GroupNames
OPEN db_cursor   
FETCH NEXT FROM db_cursor INTO @GroupName   

WHILE @@FETCH_STATUS = 0   
BEGIN   
	INSERT INTO [GroupMembership]
			   ([CreationDate]
			   ,[CreatedById]
			   ,[ForProduct]
			   ,[GroupName]
			   ,[StartDate]
			   ,[EndDate]
			   ,[User_Id]
			   ,[ApprovedDate]
			   ,[ApprovedById])
		 VALUES
			   (@initialDate
			   ,@createdByUserId
			   ,@ProductName		   
			   ,@GroupName
			   ,DATEADD(day, -1, cast(getdate() as date))
			   ,DATEADD(day, 10 * 365, cast(getdate() as date))
			   ,@userId
			   ,@initialDate
			   ,@createdByUserId)

       FETCH NEXT FROM db_cursor INTO @GroupName   
END   

CLOSE db_cursor   
DEALLOCATE db_cursor";

            return ScriptPattern
                .Replace("[[USER_DISPLAY_NAME]]", userDisplayName)
                .Replace("[[USER_IDENTITY]]", azureObjectId);
        }
    }
}
