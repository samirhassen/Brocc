using System.Security.Cryptography;
using System.Text;

namespace NTechDbTool
{
    public static class SystemUserCreator
    {
        private const int Rounds = 10000; //This CAN be safely changed without breaking stored passwords
        private static int SaltSize = 20;//This CAN be safely changed without breaking stored passwords

        private const int HashSize = 20; //This CANNOT be safely changed without breaking stored passwords
        private static Encoding enc = Encoding.UTF8; //This CANNOT be safely changed without breaking stored passwords

        private static string Hash(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                throw new ArgumentException("plainTextPassword");

#pragma warning disable SYSLIB0023 // Type or member is obsolete
            //NOTE: This needs to be changed in the core solution before it can be changed here
            using (var rng = new RNGCryptoServiceProvider())
            {
                var salt = new byte[SaltSize];
                rng.GetBytes(salt);

                var rounds = Rounds;
                using (var hasher = new Rfc2898DeriveBytes(enc.GetBytes(plainTextPassword), salt, rounds))
                {
                    var hash = hasher.GetBytes(HashSize);
                    return $"{rounds};{Convert.ToBase64String(salt)};{Convert.ToBase64String(hash)}";
                }
            }
#pragma warning restore SYSLIB0023 // Type or member is obsolete
        }

        private static string GenerateRandomPassword(int length = 20)
        {
            const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();
            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = Chars[random.Next(Chars.Length)];
            }
            return new string(stringChars);
        }

        public static int CreateScript()
        {
            const string ScriptPattern =
            @"declare @userDisplayName nvarchar(100) = 'System'
declare @userName nvarchar(128) = '[[USERNAME]]'
declare @hashedPassword nvarchar(1024) = '[[HASHED_PASSWORD]]'
declare @initialDate datetime = GETDATE()
declare @createdByUserId int = isnull((select min(Id) from [User]), 0)
declare @authenticationType nvarchar(128) = 'UsernamePassword'
declare @authenticationProvider nvarchar(128) = 'Local'
declare @userId int

--User
insert into [User] (CreationDate, CreatedById, DisplayName, IsSystemUser) values (@initialDate, @createdByUserId, @userDisplayName, 1)
select @userId = scope_identity()

--Login
INSERT INTO [AuthenticationMechanism]
           ([CreationDate]
           ,[CreatedById]
           ,[UserIdentity]
           ,[AuthenticationType]
           ,[AuthenticationProvider]
           ,[UserId]
		   ,[Credentials])
     VALUES
           (@initialDate
           ,@createdByUserId
           ,@userName
           ,@authenticationType
           ,@authenticationProvider
           ,@userId,
		    @hashedPassword)";
            /*
             
declare @userDisplayName nvarchar(100) = 'System'
declare @userName nvarchar(128) = 'systemuser1'
declare @hashedPassword nvarchar(1024) = '10000;SV6cLlUQ7X686rzRXiYJdPuULs0=;sf/wRqHNH+ZwsgelkHnOk/nHY14='

declare @initialDate datetime = GETDATE()
declare @createdByUserId int = isnull((select min(Id) from [User]), 0)
declare @authenticationType nvarchar(128) = 'UsernamePassword'
declare @authenticationProvider nvarchar(128) = 'Local'
declare @userId int
--User
insert into [User] (CreationDate, CreatedById, DisplayName, IsSystemUser) values (@initialDate, @createdByUserId, @userDisplayName, 1)
select @userId = scope_identity()

--Login
INSERT INTO [AuthenticationMechanism]
           ([CreationDate]
           ,[CreatedById]
           ,[UserIdentity]
           ,[AuthenticationType]
           ,[AuthenticationProvider]
           ,[UserId]
                   ,[Credentials])
     VALUES
           (@initialDate
           ,@createdByUserId
           ,@userName
           ,@authenticationType
           ,@authenticationProvider
           ,@userId,
            @hashedPassword)             
             
             */
            var username = "systemuser1";
            var password = GenerateRandomPassword();
            var hashedPassword = Hash(password);
            var script = ScriptPattern.Replace("[[HASHED_PASSWORD]]", hashedPassword).Replace("[[USERNAME]]", username);
            Console.WriteLine();
            Console.WriteLine("Username:");
            Console.WriteLine(username);
            Console.WriteLine();
            Console.WriteLine("Cleartext password:");
            Console.WriteLine(password);
            Console.WriteLine();
            Console.WriteLine("--Script--");
            Console.WriteLine(script);

            return 0;
        }
    }
}
