using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WildPay.Interfaces;
using WildPay.Models.Entities;
using WildPay.Models.ViewModels;
using WildPay.Helpers;

namespace WildPay.Controllers;

// methods are only accessible if the user is connected
[Authorize]
public class GroupController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGroupRepository _groupRepository;

    public GroupController(UserManager<ApplicationUser> userManager, IGroupRepository groupRepository)
    {
        _userManager = userManager;
        _groupRepository = groupRepository;
    }

    // READ: get all the groups for the connected user
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = _userManager.GetUserId(User);
        if (ManageBadRequests.IsUserIdNull(userId)) return RedirectToAction(default);

        var groups = await _groupRepository.GetGroupsAsync(userId);
        return View(groups);
    }

    [HttpGet]
    public async Task<IActionResult> Index(int Id)
    {
        //Get the group
        Group? group = await _groupRepository.GetGroupByIdAsync(Id);

        ViewBag.Message = ManageBadRequests.CheckGroupRequest(group, _userManager.GetUserId(User));
        if (!String.IsNullOrEmpty(ViewBag.Message)) return View("~/Views/Shared/CustomizedError");

        return View(group);
    }

    // CREATE group view
    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    // CREATE group action
    [HttpPost]
    public async Task<IActionResult> Add(Group group)
    {
        string? userId = _userManager.GetUserId(User);

        if (ManageBadRequests.IsUserIdNull(userId))
        {
            ViewBag.Message = "Vous n'êtes pas connecté.";
            return View("~Views/Shared/CustomizedError");
        }

        await _groupRepository.AddGroupAsync(group.Name, group.Image, userId);
        
        return RedirectToAction(actionName: "List", controllerName: "Group");
    }

    // UPDATE group view
    [HttpGet]
    public async Task<IActionResult> Update(int Id)
    {
        Group? group = await _groupRepository.GetGroupByIdAsync(Id);

        ViewBag.Message = ManageBadRequests.CheckGroupRequest(group, _userManager.GetUserId(User));
        if (!String.IsNullOrEmpty(ViewBag.Message)) return View("~/Views/Shared/CustomizedError");

        UpdateGroupModel updateGroupModel = new UpdateGroupModel
        {
            GroupToUpdate = group,
            NewMember = new MemberAdded()
            {
                GroupId = Id,
                Email = ""
            }
        };

        return View(updateGroupModel);
    }

    // UPDATE group action
    [HttpPost]
    public async Task<IActionResult> Update(UpdateGroupModel modelUpdated)
    {
        Group? groupUpdated = modelUpdated.GroupToUpdate;

        if (ManageBadRequests.IsGroupNull(groupUpdated)) return RedirectToAction(default);

        if (groupUpdated.Image is null)
        {
            groupUpdated.Image = string.Empty;
        }

        await _groupRepository.EditGroupAsync(groupUpdated);
        return RedirectToAction(actionName: "List", controllerName: "Group");
    }

    // Add a member to a group using a form
    // Make sure to add a hidden field for the group ID
    [HttpPost]
    public async Task<IActionResult> AddMember(UpdateGroupModel modelUpdated)
    {
        if (modelUpdated.NewMember is null) return NotFound();

        MemberAdded newMember = modelUpdated.NewMember;
        if (newMember is null) return RedirectToAction(default);

        // maybe can be replaced by the check model.state
        if (newMember.Email is null) return NotFound();

        Group? group = await _groupRepository.GetGroupByIdAsync(newMember.GroupId);

        if (!ManageBadRequests.IsGroupRequestCorrect(group, _userManager.GetUserId(User))) return NotFound();

        // Returns false if no match is found;
        // think about a way to handle the case the email doesn't match a user
        await _groupRepository.AddMemberToGroupAsync(group, newMember.Email);

        return RedirectToAction(actionName: "Update", controllerName: "Group", new { Id = newMember.GroupId });    
    }

    // Delete a member from a group view
    [HttpGet]
    public async Task<IActionResult> DeleteMember(string userId, int groupId)
    {
        Group? group = await _groupRepository.GetGroupByIdAsync(groupId);
        if (group is null) return NotFound();

        ApplicationUser? userToRemove = group.ApplicationUsers.FirstOrDefault(u => u.Id == userId);

        if (ManageBadRequests.IsUserNull(userToRemove)) return NotFound();

        if (!ManageBadRequests.IsGroupRequestCorrect(group, _userManager.GetUserId(User))) return NotFound();

        ViewBag.user = userToRemove;
        return View("DeleteMember", group);
    }

    // Delete a member from a group action
    [HttpPost]
    public async Task<IActionResult> DeleteMember(string userId, int groupId, Group group)
    {
        Group? userGroup = await _groupRepository.GetGroupByIdAsync(groupId);

        if (!ManageBadRequests.IsGroupRequestCorrect(group, _userManager.GetUserId(User))) return NotFound();

        // Returns false if no match is found;
        // think about a way to handle the case the email doesn't match a user
        await _groupRepository.DeleteMemberFromGroupAsync(userGroup, userId);

        return RedirectToAction(actionName: "Update", controllerName: "Group", new { Id = groupId });
    }

    // DELETE group view
    [HttpGet]
    public async Task<IActionResult> Delete(int Id)
    {
        Group? group = await _groupRepository.GetGroupByIdAsync(Id);

        if (!ManageBadRequests.IsGroupRequestCorrect(group, _userManager.GetUserId(User))) return NotFound();

        return View(group);
    }

    // DELETE group action 
    [HttpPost]
    public async Task<IActionResult> Delete(int Id, Group group)
    {
        await _groupRepository.DeleteGroupAsync(Id);
        return RedirectToAction(actionName: "List", controllerName: "Group");
    }
}