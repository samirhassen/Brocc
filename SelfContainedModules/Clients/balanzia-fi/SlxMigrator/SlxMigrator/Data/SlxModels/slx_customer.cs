using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class slx_customer
    {
		public static Dictionary<int, JObject> CreateForCustomers(HashSet<int> customerIds, ConnectionFactory connectionFactory, string encryptionKeyName, string encryptionKeyValue)
		{
			using (var creditConnection = connectionFactory.CreateOpenConnection(DatabaseCode.Customer))
			{
				var loans = creditConnection.Query<object>(@"
With CurrentDecryptedProperty
as
(
	select	p.CustomerId,
			p.[Name],
			case 
				when p.IsEncrypted = 0 then p.[Value]
				when e.EncryptionKeyName = @encryptionKeyName then convert(nvarchar(max), DecryptByPassphrase(@encryptionKeyValue, e.[Value]))
				else null 
			end as [Value]
	from	CustomerProperty p
	left outer join EncryptedValue e on (case when p.IsEncrypted = 1 then cast(p.[Value] as bigint) else -1 end) = e.Id
	where	p.IsCurrentData = 1
),
CustomerHeader
as
(
	select	p1.CustomerId as customer_id,
			p1.[Value] as legal_entity_number,
			(select top 1 p.[Value] from CurrentDecryptedProperty p where p.CustomerId = p1.CustomerId and p.[Name] = 'firstName') as first_name,
			(select top 1 p.[Value] from CurrentDecryptedProperty p where p.CustomerId = p1.CustomerId and p.[Name] = 'lastName') as last_name
	from	CurrentDecryptedProperty p1
	where	p1.[Name] = 'civicRegNr'
)
select	h.customer_id,
		h.customer_id  as customer_number,
		cast(0 as bit) as is_corporate,
		h.first_name + ' ' + h.last_name as full_name,
		h.first_name,
		h.last_name,
		(select top 1 p.[Value] from CurrentDecryptedProperty p where p.CustomerId = h.customer_id and p.[Name] = 'email') as email,
		h.legal_entity_number,
		'private' as legal_entity_type,
		(select top 1 p.[Value] from CurrentDecryptedProperty p where p.CustomerId = h.customer_id and p.[Name] = 'addressStreet') as street_address,
		(select top 1 p.[Value] from CurrentDecryptedProperty p where p.CustomerId = h.customer_id and p.[Name] = 'addressZipcode') as zip_code,
		(select top 1 p.[Value] from CurrentDecryptedProperty p where p.CustomerId = h.customer_id and p.[Name] = 'addressCity') as city,
		(select top 1 p.[Value] from CurrentDecryptedProperty p where p.CustomerId = h.customer_id and p.[Name] = 'phone') as tel,
		'' as contact_person_name,
		'' as contact_person_tel,
		'' as contact_person_email,
		'' as contact_person_id_number,
		'' as bank_name,
		'' as bank_clearing_number,
		'' as bank_account_number,
		'' as customer_origin,
		0 active
from	CustomerHeader h
where	h.customer_id in @customerIds", param: new { customerIds, encryptionKeyName, encryptionKeyValue }, commandTimeout: 60000).Select(JObject.FromObject).ToList();

				return loans
					.GroupBy(x => x["customer_id"].Value<int>())
					.ToDictionary(x => x.Key, x => x.Single());
			}
		}
	}
}
