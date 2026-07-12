using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Radzen.Blazor;

namespace KonXProWebApp.Components.Pages
{
    public partial class HomeImprovementContractors
    {
        [Inject]
        protected db_9f8bee_konxdevContext DbContext { get; set; }

        private IEnumerable<HomeImprovementContractor> contractors;
        private RadzenDataGrid<HomeImprovementContractor> grid;
        private bool isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            isLoading = true;
            contractors = await DbContext.HomeImprovementContractors
                .AsNoTracking()
                .OrderByDescending(c => c.IngestedAt)
                .ToListAsync();
            isLoading = false;
        }
    }
}
