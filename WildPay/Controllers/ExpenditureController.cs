using Microsoft.AspNetCore.Mvc;

namespace WildPay.Controllers;

public class ExpenditureController : Controller
{
    // GET
    public IActionResult List()
    {
        return View();
    }
}