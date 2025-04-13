using KonXProWebApp.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Radzen;
using System.Data;
using System.Linq.Dynamic.Core;
using System.Text.Encodings.Web;

namespace KonXProWebApp
{
    public partial class db_9f8bee_konxdevService
    {
        db_9f8bee_konxdevContext Context
        {
            get
            {
                return this.context;
            }
        }

        private readonly db_9f8bee_konxdevContext context;
        private readonly NavigationManager navigationManager;

        public db_9f8bee_konxdevService(db_9f8bee_konxdevContext context, NavigationManager navigationManager)
        {
            this.context = context;
            this.navigationManager = navigationManager;
        }

        public void Reset() => Context.ChangeTracker.Entries().Where(e => e.Entity != null).ToList().ForEach(e => e.State = EntityState.Detached);

        public void ApplyQuery<T>(ref IQueryable<T> items, Query query = null)
        {
            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Filter))
                {
                    if (query.FilterParameters != null)
                    {
                        items = items.Where(query.Filter, query.FilterParameters);
                    }
                    else
                    {
                        items = items.Where(query.Filter);
                    }
                }

                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    items = items.OrderBy(query.OrderBy);
                }

                if (query.Skip.HasValue)
                {
                    items = items.Skip(query.Skip.Value);
                }

                if (query.Top.HasValue)
                {
                    items = items.Take(query.Top.Value);
                }
            }
        }


        public async Task ExportBlogContentsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/blogcontents/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/blogcontents/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportBlogContentsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/blogcontents/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/blogcontents/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnBlogContentsRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent>> GetBlogContents(Query query = null)
        {
            var items = Context.BlogContents.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnBlogContentsRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnBlogContentGet(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent item);
        partial void OnGetBlogContentByContentId(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent> items);


        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent> GetBlogContentByContentId(int contentid)
        {
            var items = Context.BlogContents
                              .AsNoTracking()
                              .Where(i => i.ContentId == contentid);


            OnGetBlogContentByContentId(ref items);

            var itemToReturn = items.FirstOrDefault();

            OnBlogContentGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnBlogContentCreated(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent item);
        partial void OnAfterBlogContentCreated(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent> CreateBlogContent(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent blogcontent)
        {
            OnBlogContentCreated(blogcontent);

            var existingItem = Context.BlogContents
                              .Where(i => i.ContentId == blogcontent.ContentId)
                              .FirstOrDefault();

            if (existingItem != null)
            {
                throw new Exception("Item already available");
            }

            try
            {
                Context.BlogContents.Add(blogcontent);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(blogcontent).State = EntityState.Detached;
                throw;
            }

            OnAfterBlogContentCreated(blogcontent);

            return blogcontent;
        }

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent> CancelBlogContentChanges(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
                entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
                entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnBlogContentUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent item);
        partial void OnAfterBlogContentUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent> UpdateBlogContent(int contentid, KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent blogcontent)
        {
            OnBlogContentUpdated(blogcontent);

            var itemToUpdate = Context.BlogContents
                              .Where(i => i.ContentId == blogcontent.ContentId)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
                throw new Exception("Item no longer available");
            }

            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(blogcontent);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterBlogContentUpdated(blogcontent);

            return blogcontent;
        }

        partial void OnBlogContentDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent item);
        partial void OnAfterBlogContentDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent> DeleteBlogContent(int contentid)
        {
            var itemToDelete = Context.BlogContents
                              .Where(i => i.ContentId == contentid)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
                throw new Exception("Item no longer available");
            }

            OnBlogContentDeleted(itemToDelete);


            Context.BlogContents.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterBlogContentDeleted(itemToDelete);

            return itemToDelete;
        }

        public async Task ExportBlogFeedSourcesToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/blogfeedsources/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/blogfeedsources/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportBlogFeedSourcesToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/blogfeedsources/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/blogfeedsources/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnBlogFeedSourcesRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource>> GetBlogFeedSources(Query query = null)
        {
            var items = Context.BlogFeedSources.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnBlogFeedSourcesRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnBlogFeedSourceGet(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource item);
        partial void OnGetBlogFeedSourceByFeedId(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> items);


        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> GetBlogFeedSourceByFeedId(int feedid)
        {
            var items = Context.BlogFeedSources
                              .AsNoTracking()
                              .Where(i => i.FeedId == feedid);


            OnGetBlogFeedSourceByFeedId(ref items);

            var itemToReturn = items.FirstOrDefault();

            OnBlogFeedSourceGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnBlogFeedSourceCreated(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource item);
        partial void OnAfterBlogFeedSourceCreated(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> CreateBlogFeedSource(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource blogfeedsource)
        {
            OnBlogFeedSourceCreated(blogfeedsource);

            var existingItem = Context.BlogFeedSources
                              .Where(i => i.FeedId == blogfeedsource.FeedId)
                              .FirstOrDefault();

            if (existingItem != null)
            {
                throw new Exception("Item already available");
            }

            try
            {
                Context.BlogFeedSources.Add(blogfeedsource);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(blogfeedsource).State = EntityState.Detached;
                throw;
            }

            OnAfterBlogFeedSourceCreated(blogfeedsource);

            return blogfeedsource;
        }

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> CancelBlogFeedSourceChanges(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
                entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
                entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnBlogFeedSourceUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource item);
        partial void OnAfterBlogFeedSourceUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> UpdateBlogFeedSource(int feedid, KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource blogfeedsource)
        {
            OnBlogFeedSourceUpdated(blogfeedsource);

            var itemToUpdate = Context.BlogFeedSources
                              .Where(i => i.FeedId == blogfeedsource.FeedId)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
                throw new Exception("Item no longer available");
            }

            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(blogfeedsource);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterBlogFeedSourceUpdated(blogfeedsource);

            return blogfeedsource;
        }

        partial void OnBlogFeedSourceDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource item);
        partial void OnAfterBlogFeedSourceDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> DeleteBlogFeedSource(int feedid)
        {
            var itemToDelete = Context.BlogFeedSources
                              .Where(i => i.FeedId == feedid)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
                throw new Exception("Item no longer available");
            }

            OnBlogFeedSourceDeleted(itemToDelete);


            Context.BlogFeedSources.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterBlogFeedSourceDeleted(itemToDelete);

            return itemToDelete;
        }

        public async Task ExportDobViolationsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/dobviolations/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/dobviolations/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportDobViolationsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/dobviolations/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/dobviolations/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnDobViolationsRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation>> GetDobViolations(Query query = null)
        {
            var items = Context.DobViolations.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnDobViolationsRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnDobViolationGet(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation item);
        partial void OnGetDobViolationById(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> items);


        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> GetDobViolationById(int id)
        {
            var items = Context.DobViolations
                              .AsNoTracking()
                              .Where(i => i.Id == id);


            OnGetDobViolationById(ref items);

            var itemToReturn = items.FirstOrDefault();

            OnDobViolationGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnDobViolationCreated(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation item);
        partial void OnAfterDobViolationCreated(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> CreateDobViolation(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation dobviolation)
        {
            OnDobViolationCreated(dobviolation);

            var existingItem = Context.DobViolations
                              .Where(i => i.Id == dobviolation.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
                throw new Exception("Item already available");
            }

            try
            {
                Context.DobViolations.Add(dobviolation);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(dobviolation).State = EntityState.Detached;
                throw;
            }

            OnAfterDobViolationCreated(dobviolation);

            return dobviolation;
        }

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> CancelDobViolationChanges(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
                entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
                entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnDobViolationUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation item);
        partial void OnAfterDobViolationUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> UpdateDobViolation(int id, KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation dobviolation)
        {
            OnDobViolationUpdated(dobviolation);

            var itemToUpdate = Context.DobViolations
                              .Where(i => i.Id == dobviolation.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
                throw new Exception("Item no longer available");
            }

            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(dobviolation);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterDobViolationUpdated(dobviolation);

            return dobviolation;
        }

        partial void OnDobViolationDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation item);
        partial void OnAfterDobViolationDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> DeleteDobViolation(int id)
        {
            var itemToDelete = Context.DobViolations
                              .Where(i => i.Id == id)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
                throw new Exception("Item no longer available");
            }

            OnDobViolationDeleted(itemToDelete);


            Context.DobViolations.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterDobViolationDeleted(itemToDelete);

            return itemToDelete;
        }

        public async Task ExportDobjobFilingsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/dobjobfilings/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/dobjobfilings/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportDobjobFilingsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/dobjobfilings/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/dobjobfilings/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnDobjobFilingsRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>> GetDobjobFilings(Query query = null)
        {
            var items = Context.DobjobFilings.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnDobjobFilingsRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnDobjobFilingGet(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling item);
        partial void OnGetDobjobFilingById(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> items);


        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> GetDobjobFilingById(int id)
        {
            var items = Context.DobjobFilings
                              .AsNoTracking()
                              .Where(i => i.JobNum == id);


            OnGetDobjobFilingById(ref items);

            var itemToReturn = items.FirstOrDefault();

            OnDobjobFilingGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnDobjobFilingCreated(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling item);
        partial void OnAfterDobjobFilingCreated(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> CreateDobjobFiling(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling dobjobfiling)
        {
            OnDobjobFilingCreated(dobjobfiling);

            var existingItem = Context.DobjobFilings
                              .Where(i => i.Id == dobjobfiling.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
                throw new Exception("Item already available");
            }

            try
            {
                Context.DobjobFilings.Add(dobjobfiling);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(dobjobfiling).State = EntityState.Detached;
                throw;
            }

            OnAfterDobjobFilingCreated(dobjobfiling);

            return dobjobfiling;
        }

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> CancelDobjobFilingChanges(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
                entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
                entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnDobjobFilingUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling item);
        partial void OnAfterDobjobFilingUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> UpdateDobjobFiling(int id, KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling dobjobfiling)
        {
            OnDobjobFilingUpdated(dobjobfiling);

            var itemToUpdate = Context.DobjobFilings
                              .Where(i => i.Id == dobjobfiling.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
                throw new Exception("Item no longer available");
            }

            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(dobjobfiling);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterDobjobFilingUpdated(dobjobfiling);

            return dobjobfiling;
        }

        partial void OnDobjobFilingDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling item);
        partial void OnAfterDobjobFilingDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> DeleteDobjobFiling(int id)
        {
            var itemToDelete = Context.DobjobFilings
                              .Where(i => i.Id == id)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
                throw new Exception("Item no longer available");
            }

            OnDobjobFilingDeleted(itemToDelete);


            Context.DobjobFilings.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterDobjobFilingDeleted(itemToDelete);

            return itemToDelete;
        }

        public async Task ExportEcbViolationsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/ecbviolations/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/ecbviolations/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportEcbViolationsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/ecbviolations/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/ecbviolations/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnEcbViolationsRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation>> GetEcbViolations(Query query = null)
        {
            var items = Context.EcbViolations.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnEcbViolationsRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnEcbViolationGet(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation item);
        partial void OnGetEcbViolationById(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> items);


        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> GetEcbViolationById(int id)
        {
            var items = Context.EcbViolations
                              .AsNoTracking()
                              .Where(i => i.Id == id);


            OnGetEcbViolationById(ref items);

            var itemToReturn = items.FirstOrDefault();

            OnEcbViolationGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnEcbViolationCreated(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation item);
        partial void OnAfterEcbViolationCreated(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> CreateEcbViolation(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation ecbviolation)
        {
            OnEcbViolationCreated(ecbviolation);

            var existingItem = Context.EcbViolations
                              .Where(i => i.Id == ecbviolation.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
                throw new Exception("Item already available");
            }

            try
            {
                Context.EcbViolations.Add(ecbviolation);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(ecbviolation).State = EntityState.Detached;
                throw;
            }

            OnAfterEcbViolationCreated(ecbviolation);

            return ecbviolation;
        }

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> CancelEcbViolationChanges(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
                entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
                entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnEcbViolationUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation item);
        partial void OnAfterEcbViolationUpdated(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> UpdateEcbViolation(int id, KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation ecbviolation)
        {
            OnEcbViolationUpdated(ecbviolation);

            var itemToUpdate = Context.EcbViolations
                              .Where(i => i.Id == ecbviolation.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
                throw new Exception("Item no longer available");
            }

            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(ecbviolation);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterEcbViolationUpdated(ecbviolation);

            return ecbviolation;
        }

        partial void OnEcbViolationDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation item);
        partial void OnAfterEcbViolationDeleted(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation item);

        public async Task<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> DeleteEcbViolation(int id)
        {
            var itemToDelete = Context.EcbViolations
                              .Where(i => i.Id == id)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
                throw new Exception("Item no longer available");
            }

            OnEcbViolationDeleted(itemToDelete);


            Context.EcbViolations.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterEcbViolationDeleted(itemToDelete);

            return itemToDelete;
        }

        public async Task ExportVwBasicTierDashboardsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwbasictierdashboards/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwbasictierdashboards/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportVwBasicTierDashboardsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwbasictierdashboards/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwbasictierdashboards/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnVwBasicTierDashboardsRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwBasicTierDashboard> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwBasicTierDashboard>> GetVwBasicTierDashboards(Query query = null)
        {
            var items = Context.VwBasicTierDashboards.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnVwBasicTierDashboardsRead(ref items);

            return await Task.FromResult(items);
        }

        public async Task ExportVwDemoDisplaysToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwdemodisplays/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwdemodisplays/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportVwDemoDisplaysToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwdemodisplays/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwdemodisplays/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnVwDemoDisplaysRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwDemoDisplay> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwDemoDisplay>> GetVwDemoDisplays(Query query = null)
        {
            var items = Context.VwDemoDisplays.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnVwDemoDisplaysRead(ref items);

            return await Task.FromResult(items);
        }

        public async Task ExportVwFreeTierDashboardsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwfreetierdashboards/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwfreetierdashboards/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportVwFreeTierDashboardsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwfreetierdashboards/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwfreetierdashboards/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnVwFreeTierDashboardsRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwFreeTierDashboard> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwFreeTierDashboard>> GetVwFreeTierDashboards(Query query = null)
        {
            var items = Context.VwFreeTierDashboards.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnVwFreeTierDashboardsRead(ref items);

            return await Task.FromResult(items);
        }

        public async Task ExportVwHighTierDashboardsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwhightierdashboards/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwhightierdashboards/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportVwHighTierDashboardsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwhightierdashboards/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwhightierdashboards/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnVwHighTierDashboardsRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard>> GetVwHighTierDashboards(Query query = null)
        {
            var items = Context.VwHighTierDashboards.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnVwHighTierDashboardsRead(ref items);

            return await Task.FromResult(items);
        }

        public async Task ExportVwMidTierDashboardsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwmidtierdashboards/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwmidtierdashboards/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportVwMidTierDashboardsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/db_9f8bee_konxdev/vwmidtierdashboards/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/db_9f8bee_konxdev/vwmidtierdashboards/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnVwMidTierDashboardsRead(ref IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwMidTierDashboard> items);

        public async Task<IQueryable<KonXProWebApp.Models.db_9f8bee_konxdev.VwMidTierDashboard>> GetVwMidTierDashboards(Query query = null)
        {
            var items = Context.VwMidTierDashboards.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach (var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                ApplyQuery(ref items, query);
            }

            OnVwMidTierDashboardsRead(ref items);

            return await Task.FromResult(items);
        }
    }
}