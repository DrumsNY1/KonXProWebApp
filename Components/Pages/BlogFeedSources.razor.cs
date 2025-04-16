using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace KonXProWebApp.Components.Pages
{
    public partial class BlogFeedSources
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public db_9f8bee_konxdevService db_9f8bee_konxdevService { get; set; }

        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> blogFeedSources;

        protected RadzenDataGrid<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> grid0;
        protected bool isEdit = true;

        protected string search = "";

        protected async Task Search(ChangeEventArgs args)
        {
            search = $"{args.Value}";

            await grid0.GoToPage(0);

            blogFeedSources = await db_9f8bee_konxdevService.GetBlogFeedSources(new Query { Filter = $@"i => i.FeedName.Contains(@0) || i.FeedUrl.Contains(@0) || i.FeedCategory.Contains(@0)", FilterParameters = new object[] { search } });
        }
        protected override async Task OnInitializedAsync()
        {
            blogFeedSources = await db_9f8bee_konxdevService.GetBlogFeedSources(new Query { Filter = $@"i => i.FeedName.Contains(@0) || i.FeedUrl.Contains(@0) || i.FeedCategory.Contains(@0)", FilterParameters = new object[] { search } });
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await grid0.SelectRow(null);
            isEdit = false;
            blogFeedSource = new KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource();
        }

        protected async Task EditRow(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource args)
        {
            isEdit = true;
            blogFeedSource = args;
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource blogFeedSource)
        {
            try
            {
                if (await DialogService.Confirm("Are you sure you want to delete this record?") == true)
                {
                    var deleteResult = await db_9f8bee_konxdevService.DeleteBlogFeedSource(blogFeedSource.FeedId);

                    if (deleteResult != null)
                    {
                        await grid0.Reload();
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = $"Error",
                    Detail = $"Unable to delete BlogFeedSource"
                });
            }
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            if (args?.Value == "csv")
            {
                await db_9f8bee_konxdevService.ExportBlogFeedSourcesToCSV(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "BlogFeedSources");
            }

            if (args == null || args.Value == "xlsx")
            {
                await db_9f8bee_konxdevService.ExportBlogFeedSourcesToExcel(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "BlogFeedSources");
            }
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource blogFeedSource;

        [Inject]
        protected SecurityService Security { get; set; }

        protected async Task FormSubmit()
        {
            try
            {
                var result = isEdit ? await db_9f8bee_konxdevService.UpdateBlogFeedSource(blogFeedSource.FeedId, blogFeedSource) : await db_9f8bee_konxdevService.CreateBlogFeedSource(blogFeedSource);

            }
            catch (Exception ex)
            {
                errorVisible = true;
            }
        }

        protected async Task CancelButtonClick(MouseEventArgs args)
        {

        }
    }
}