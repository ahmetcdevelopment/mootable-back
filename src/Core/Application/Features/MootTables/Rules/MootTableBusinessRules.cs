using Mootable.Application.Features.MootTables.Constants;
using Mootable.Domain.Entities;
using Mootable.Domain.Exceptions;

namespace Mootable.Application.Features.MootTables.Rules;

public sealed class MootTableBusinessRules
{
    public void MootTableMustExist(MootTable? mootTable)
    {
        if (mootTable == null || mootTable.IsDeleted)
        {
            throw new BusinessRuleException("MT_001", MootTableMessages.MootTableNotFound);
        }
    }

    public void MootTableMustNotBeArchived(MootTable mootTable)
    {
        if (mootTable.IsArchived)
        {
            throw new BusinessRuleException("MT_002", MootTableMessages.MootTableArchived);
        }
    }

    public void CannotDeleteDefaultMootTable(MootTable mootTable)
    {
        if (mootTable.Name.Equals("general", StringComparison.OrdinalIgnoreCase) && mootTable.Position == 0)
        {
            throw new BusinessRuleException("MT_003", MootTableMessages.CannotDeleteDefaultMootTable);
        }
    }
}
