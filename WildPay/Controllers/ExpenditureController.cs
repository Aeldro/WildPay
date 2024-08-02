using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using WildPay.Interfaces;
using WildPay.Models;
using WildPay.Models.Entities;

namespace WildPay.Controllers;

[Authorize]
public class ExpenditureController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IGroupRepository _groupRepository;

    public ExpenditureController(UserManager<ApplicationUser> userManager, IExpenditureRepository expenditureRepository, IGroupRepository groupRepository)
    {
        _userManager = userManager;
        _expenditureRepository = expenditureRepository;
        _groupRepository = groupRepository;
    }

    // READ
    async public Task<IActionResult> ListGroupExpenditures(int Id)
    {
        //Get the group
        Group? group = await _groupRepository.GetGroupByIdAsync(Id);

        //Return not found if no group is found
        if (group == null) { return NotFound(); }

        //Verify if the User belongs to the group, else we block the access
        if (_userManager.GetUserId(User) is null || group.ApplicationUsers.FirstOrDefault(el => el.Id == _userManager.GetUserId(User)) is null) { return NotFound(); }

        return View(group);
    }

    async public Task<IActionResult> ListGroupBalances(int Id)
    {
        //Get the group
        Group? group = await _groupRepository.GetGroupByIdAsync(Id);

        //Return not found if no group is found
        if (group == null) { return NotFound(); }

        //Verify if the User belongs to the group, else we block the access
        if (_userManager.GetUserId(User) is null || group.ApplicationUsers.FirstOrDefault(el => el.Id == _userManager.GetUserId(User)) is null) { return NotFound(); }

        //Init GroupBalance instance
        GroupBalance groupBalance = new GroupBalance();
        groupBalance.Group = group;
        groupBalance.TotalAmount = group.Expenditures.Sum(el => el.Amount);

        Dictionary<string, double> membersBalance = CalculateMembersBalance(group); //Calculate the balance of each member
        membersBalance.OrderBy(el => el.Value);
        groupBalance.UsersBalance = membersBalance;
        groupBalance = CalculateDebtsList(groupBalance, group); //Calculate who must pay who

        if (group.Expenditures.Any(el => el.PayerId is null) || group.Expenditures.Any(el => el.Payer is null)) groupBalance.Message = "Attention ! Les d�penses qui n'ont pas de payeur n'ont pas �t� prises en compte. V�rifiez les d�penses du groupe et ajoutez-y un payeur si vous voulez les inclure au calcul.";
        else if (groupBalance.Debts.Count > 0 && groupBalance.Message == "") groupBalance.Message = "Calcul effectu� avec succ�s.";
        else if (groupBalance.Debts.Count == 0 && groupBalance.Message == "") groupBalance.Message = "Aucun remboursement � effectuer.";

        return View(groupBalance);
    }

    //Used in the GroupBalances method to calculate the balance of each member
    public Dictionary<string, double> CalculateMembersBalance(Group group)
    {
        Dictionary<string, double> membersBalance = new Dictionary<string, double>();

        foreach (ApplicationUser member in group.ApplicationUsers) membersBalance.Add(member.Id, 0);

        foreach (Expenditure expenditure in group.Expenditures)
        {
            if (expenditure.PayerId is not null && expenditure.Payer is not null)
            {
                double expenditureContributionPerPerson = expenditure.Amount / expenditure.RefundContributors.Count;
                membersBalance[expenditure.PayerId] = membersBalance[expenditure.PayerId] + expenditure.Amount;
                foreach (ApplicationUser contributor in expenditure.RefundContributors)
                {
                    membersBalance[contributor.Id] = membersBalance[contributor.Id] - expenditureContributionPerPerson;
                }
            }
        }

        return membersBalance;
    }

    //Used in the GroupBalances method to calculate who must pay who
    public GroupBalance CalculateDebtsList(GroupBalance groupBalance, Group group)
    {
        //Split the users balances into two parts : positives balances and negative balances. This aims to make the further calculation easier
        Dictionary<string, double> positiveBalanceMembers = new Dictionary<string, double>();
        Dictionary<string, double> negativeBalanceMembers = new Dictionary<string, double>();
        foreach (KeyValuePair<string, double> member in groupBalance.UsersBalance)
        {
            if (member.Value < 0) negativeBalanceMembers.Add(member.Key, member.Value);
            else if (member.Value > 0) positiveBalanceMembers.Add(member.Key, member.Value);
        }

        //Looping until everyone gets refunded
        while (positiveBalanceMembers.Any(el => el.Value > 0.01) && negativeBalanceMembers.Any(el => el.Value < 0.01))
        {
            //Match the member who has the highest balance to the member who has the lowest balance
            KeyValuePair<string, double> positiveBalanceMember = positiveBalanceMembers.OrderByDescending(el => el.Value).First();
            ApplicationUser positiveBalanceUser = group.ApplicationUsers.Find(el => el.Id == positiveBalanceMember.Key);
            KeyValuePair<string, double> negativeBalanceMember = negativeBalanceMembers.OrderBy(el => el.Value).First();
            ApplicationUser negativeBalanceUser = group.ApplicationUsers.Find(el => el.Id == negativeBalanceMember.Key);

            double amount = 0;

            //Update the balance of both members
            if (positiveBalanceMember.Value >= negativeBalanceMember.Value)
            {
                amount = -negativeBalanceMember.Value;
                positiveBalanceMembers[positiveBalanceMember.Key] = positiveBalanceMember.Value + negativeBalanceMember.Value;
                negativeBalanceMembers[negativeBalanceMember.Key] = negativeBalanceMember.Value + negativeBalanceMember.Value;
            }
            else if (positiveBalanceMember.Value < negativeBalanceMember.Value)
            {
                amount = positiveBalanceMember.Value;
                negativeBalanceMembers[negativeBalanceMember.Key] = negativeBalanceMember.Value + positiveBalanceMember.Value;
                positiveBalanceMembers[positiveBalanceMember.Key] = positiveBalanceMember.Value - positiveBalanceMember.Value;
            }

            //Make the debt
            Debt debt = new Debt(negativeBalanceUser, positiveBalanceUser, amount);
            groupBalance.Debts.Add(debt);
        }

        return groupBalance;
    }
}