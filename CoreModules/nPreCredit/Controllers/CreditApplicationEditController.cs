using nPreCredit.Code.Services;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    [RoutePrefix("CreditApplicationEdit")]
    public class CreditApplicationEditController : NController
    {
        [Route("EditValue")]
        public ActionResult EditValue(string mode, string applicationNr, string groupName, string name)
        {
            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();

            string value;
            if (groupName == "application")
            {
                PartialCreditApplicationModel model = repo.Get(applicationNr, applicationFields: new List<string> { name });
                value = model.Application.Get(name).StringValue.Optional;
            }
            else if (groupName == "applicant1")
            {
                PartialCreditApplicationModel model = repo.Get(applicationNr, applicantFields: new List<string> { name });
                value = model.Applicant(1).Get(name).StringValue.Optional;
            }
            else if (groupName == "applicant2")
            {
                PartialCreditApplicationModel model = repo.Get(applicationNr, applicantFields: new List<string> { name });
                value = model.Applicant(2).Get(name).StringValue.Optional;
            }
            else
            {
                throw new Exception("Unsupported group name: " + groupName);
            }

            using (var context = new PreCreditContext())
            {
                var logItems = context
                     .CreditApplicationChangeLogItems
                     .Where(x => x.ApplicationNr == applicationNr && x.Name == name && x.GroupName == groupName)
                     .ToList()
                     .Select(x => new
                     {
                         x.ChangedDate,
                         x.OldValue,
                         ChangedByName = GetUserDisplayNameByUserId(x.ChangedById.ToString())
                     })
                     .OrderByDescending(x => x.ChangedDate)
                     .ToList();

                SetInitialData(new
                {
                    translation = GetTranslations(),
                    mode = mode,
                    applicationNr = applicationNr,
                    name = name,
                    groupName = groupName,
                    value = value,
                    logItems = logItems,
                    insertValueUrl = Url.Action("InsertValue"),
                    saveEditValueUrl = Url.Action("SaveEditValue"),
                    removeValueUrl = Url.Action("RemoveValue")
                });
            }
            return View();
        }

        [Route("ViewValue")]
        public ActionResult ViewValue(string applicationNr, string groupName, string name)
        {
            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();

            string value;
            if (groupName == "application")
            {
                PartialCreditApplicationModel model = repo.Get(applicationNr, applicationFields: new List<string> { name });
                value = model.Application.Get(name).StringValue.Optional;
            }
            else if (groupName == "applicant1")
            {
                PartialCreditApplicationModel model = repo.Get(applicationNr, applicantFields: new List<string> { name });
                value = model.Applicant(1).Get(name).StringValue.Optional;
            }
            else if (groupName == "applicant2")
            {
                PartialCreditApplicationModel model = repo.Get(applicationNr, applicantFields: new List<string> { name });
                value = model.Applicant(2).Get(name).StringValue.Optional;
            }
            else
            {
                throw new Exception("Unsupported group name: " + groupName);
            }

            using (var context = new PreCreditContext())
            {
                var logItems = context
                     .CreditApplicationChangeLogItems
                     .Where(x => x.ApplicationNr == applicationNr && x.Name == name && x.GroupName == groupName)
                     .ToList()
                     .Select(x => new
                     {
                         x.ChangedDate,
                         x.OldValue,
                         ChangedByName = GetUserDisplayNameByUserId(x.ChangedById.ToString())
                     })
                     .OrderByDescending(x => x.ChangedDate)
                     .ToList();

                SetInitialData(new
                {
                    translation = GetTranslations(),
                    applicationNr = applicationNr,
                    name = name,
                    groupName = groupName,
                    value = value,
                    logItems = logItems
                });
            }
            return View();
        }

        [Route("SaveEditValue")]
        [HttpPost]
        public ActionResult SaveEditValue(string applicationNr, string name, string groupName, string value)
        {
            using (var context = Service.Resolve<IPreCreditContextFactoryService>().CreateExtended())
            {
                var isChanged = CreditApplicationItemService.SetNonEncryptedItemComposable(context, applicationNr, name, groupName, value, "EditValue");
                if(isChanged)
                {
                    context.SaveChanges();
                }
            }
            return new EmptyResult();
        }

        [Route("RemoveValue")]
        [HttpPost]
        public ActionResult RemoveValue(string applicationNr, string name, string groupName)
        {
            using (var context = new PreCreditContext())
            {
                CreditApplicationItem creditApplicationItem = context
                    .CreditApplicationItems
                    .Where(x => x.ApplicationNr == applicationNr && x.GroupName == groupName && x.Name == name)
                    .SingleOrDefault();

                if (creditApplicationItem == null)
                {
                    throw new Exception("Application item missing: " + applicationNr + "; " + name + ";" + groupName);
                }

                var logItem = new CreditApplicationChangeLogItem
                {
                    ApplicationNr = applicationNr,
                    Name = name,
                    GroupName = groupName,
                    OldValue = "-",
                    TransactionType = CreditApplicationChangeLogItem.TransactionTypeCode.Delete.ToString(),
                    ChangedById = CurrentUserId,
                    ChangedDate = Clock.Now,
                    InformationMetaData = InformationMetadata
                };
                context.CreditApplicationChangeLogItems.Add(logItem);
                context.CreditApplicationItems.Remove(creditApplicationItem);
                context.SaveChanges();
            }
            return new EmptyResult();
        }

        [Route("InsertValue")]
        [HttpPost]
        public ActionResult InsertValue(string applicationNr, string name, string groupName, string value)
        {
            using (var context = new PreCreditContext())
            {
                CreditApplicationItem creditApplicationItem = new CreditApplicationItem
                {
                    ApplicationNr = applicationNr,
                    GroupName = groupName,
                    Name = name,
                    IsEncrypted = false,
                    AddedInStepName = "UpdateApplication",
                    Value = value,
                };
                context.CreditApplicationItems.Add(creditApplicationItem);

                var logItem = new CreditApplicationChangeLogItem
                {
                    ApplicationNr = applicationNr,
                    Name = name,
                    GroupName = groupName,
                    OldValue = creditApplicationItem.Value,
                    TransactionType = CreditApplicationChangeLogItem.TransactionTypeCode.Insert.ToString(),
                    ChangedById = CurrentUserId,
                    ChangedDate = Clock.Now,
                    InformationMetaData = InformationMetadata
                };
                context.CreditApplicationChangeLogItems.Add(logItem);
                creditApplicationItem.Value = value;
                context.SaveChanges();
            }
            return new EmptyResult();
        }
    }
}