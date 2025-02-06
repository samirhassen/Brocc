using System.Collections.Generic;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public interface IPolicyFilterDataSource
    {
        VariableSet LoadVariables(ISet<string> applicationVariableNames, ISet<string> applicantVariableNames);
    }
}
