using System.Collections.Generic;

namespace nPreCredit
{
    public abstract class BaseRepository
    {
        protected readonly string currentEncryptionKeyName;
        protected readonly IDictionary<string, string> encryptionKeysByName;

        public BaseRepository(
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName)
        {
            this.currentEncryptionKeyName = currentEncryptionKeyName;
            this.encryptionKeysByName = encryptionKeysByName;
        }

        public BaseRepository() : this(NEnv.EncryptionKeys.CurrentKeyName, NEnv.EncryptionKeys.AsDictionary())
        {

        }
    }
}
