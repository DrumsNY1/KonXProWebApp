using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

using KonXProWebApp.Data;

namespace KonXProWebApp.Controllers
{
    public partial class Exportdb_9f8bee_konxdevController : ExportController
    {
        private readonly db_9f8bee_konxdevContext context;
        private readonly db_9f8bee_konxdevService service;

        public Exportdb_9f8bee_konxdevController(db_9f8bee_konxdevContext context, db_9f8bee_konxdevService service)
        {
            this.service = service;
            this.context = context;
        }

        [HttpGet("/export/db_9f8bee_konxdev/blogcontents/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/blogcontents/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportBlogContentsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetBlogContents(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/blogcontents/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/blogcontents/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportBlogContentsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetBlogContents(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/blogfeedsources/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/blogfeedsources/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportBlogFeedSourcesToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetBlogFeedSources(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/blogfeedsources/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/blogfeedsources/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportBlogFeedSourcesToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetBlogFeedSources(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/dobviolations/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/dobviolations/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportDobViolationsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetDobViolations(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/dobviolations/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/dobviolations/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportDobViolationsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetDobViolations(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/dobjobfilings/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/dobjobfilings/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportDobjobFilingsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetDobjobFilings(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/dobjobfilings/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/dobjobfilings/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportDobjobFilingsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetDobjobFilings(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/ecbviolations/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/ecbviolations/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportEcbViolationsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetEcbViolations(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/ecbviolations/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/ecbviolations/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportEcbViolationsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetEcbViolations(), Request.Query, false), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwbasictierdashboards/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/vwbasictierdashboards/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwBasicTierDashboardsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetVwBasicTierDashboards(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwbasictierdashboards/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/vwbasictierdashboards/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwBasicTierDashboardsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetVwBasicTierDashboards(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwdemodisplays/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/vwdemodisplays/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwDemoDisplaysToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetVwDemoDisplays(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwdemodisplays/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/vwdemodisplays/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwDemoDisplaysToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetVwDemoDisplays(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwfreetierdashboards/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/vwfreetierdashboards/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwFreeTierDashboardsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetVwFreeTierDashboards(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwfreetierdashboards/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/vwfreetierdashboards/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwFreeTierDashboardsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetVwFreeTierDashboards(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwhightierdashboards/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/vwhightierdashboards/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwHighTierDashboardsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetVwHighTierDashboards(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwhightierdashboards/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/vwhightierdashboards/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwHighTierDashboardsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetVwHighTierDashboards(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwmidtierdashboards/csv")]
        [HttpGet("/export/db_9f8bee_konxdev/vwmidtierdashboards/csv(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwMidTierDashboardsToCSV(string fileName = null)
        {
            return ToCSV(ApplyQuery(await service.GetVwMidTierDashboards(), Request.Query, true), fileName);
        }

        [HttpGet("/export/db_9f8bee_konxdev/vwmidtierdashboards/excel")]
        [HttpGet("/export/db_9f8bee_konxdev/vwmidtierdashboards/excel(fileName='{fileName}')")]
        public async Task<FileStreamResult> ExportVwMidTierDashboardsToExcel(string fileName = null)
        {
            return ToExcel(ApplyQuery(await service.GetVwMidTierDashboards(), Request.Query, true), fileName);
        }
    }
}
