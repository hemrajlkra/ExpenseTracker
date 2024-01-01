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
            DateTime startDate = DateTime.Today.AddDays(-7);
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

            //spline chart Income vs Expense
            //Income
            List<SplineChartData> incomeSummary = selectedTransactions.Where(x => x.Category.Type.Equals("Income"))
                .GroupBy(x => x.Date).
                Select(x => new SplineChartData()
                {
                    Day = x.First().Date.ToString("dd-MMM"),
                    Income = x.Sum(l=>l.Amount),
                }).ToList();
            //Expense
            List<SplineChartData> expenseSummary = selectedTransactions.Where(x => x.Category.Type.Equals("Expense"))
                .GroupBy(x => x.Date).
                Select(x => new SplineChartData()
                {
                    Day = x.First().Date.ToString("dd-MMM"),
                    Expense = x.Sum(l => l.Amount),
                }).ToList();
            //combine Income and Expense 
            string[] last30Days =Enumerable.Range(0,8).
                Select(i=>startDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();
            ViewBag.SplineChartData = from day in last30Days
                                      join income in incomeSummary on day equals income.Day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in expenseSummary on day equals expense.Day into dayExpenseJoined
                                      from expense in dayExpenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.Income,
                                          expense = expense == null ? 0 : expense.Expense,
                                      };
            return View();
        }
        public class SplineChartData
        {
            public string Day;
            public int Income;
            public int Expense;

        }
    }
}
