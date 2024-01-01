using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            // Last 7 days trans
            DateTime startDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Now;

            List<Transaction> selectedTransactions = await _context.Transactions
                .Include(x=>x.Category)
                .Where(y=>y.Date>=startDate && y.Date<=EndDate)
                .ToListAsync() ;
            //total Income
            int totalIncome = selectedTransactions
                .Where(x => x.Category.Type.Equals("Income"))
                .Sum(x => x.Amount);
            ViewBag.totalIncome=totalIncome.ToString("₹0.00");
            //total Expense
            int totalExpense = selectedTransactions
                .Where(x => x.Category.Type.Equals("Expense"))
                .Sum(x => x.Amount);
            ViewBag.totalExpense = totalExpense.ToString("₹0.00");
            //total Balance
            int totalBalance = totalIncome-totalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-in");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.totalBalance = string.Format(culture, "{0:₹0.00}",totalBalance) ;
            //totalBalance.ToString("₹0.00")

            // Doughnut chart Expense by category
            ViewBag.DoughnutChartData = selectedTransactions.Where(i => i.Category.Type == "Expense").GroupBy(x => x.Category.Id)
                .Select(x => new
                {
                    categoryTitleWithIcon = x.First().Category.Icon + " " + x.First().Category.Title,
                    amount = x.Sum(j => j.Amount),
                    formattedAmount = x.Sum(j => j.Amount).ToString("₹0.00")
                })
                .OrderByDescending(k=>k.amount)
                .ToList(); 
            return View();
        }
    }
}
