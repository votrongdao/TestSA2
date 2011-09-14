﻿namespace TestSA2.Web.Mvc.Controllers.Products
{
  using System.Collections.Generic;
  using System.IO;
  using System.Web.Hosting;
  using System.Web.Mvc;

  using MvcContrib;
  using MvcContrib.Filters;

  using SharpArch.Domain.Commands;
  using SharpArch.NHibernate.Web.Mvc;

  using TestSA2.Domain.Contracts.Tasks;
  using TestSA2.Tasks.Commands;
  using TestSA2.Web.Mvc.Controllers.Products.Queries;
  using TestSA2.Web.Mvc.Controllers.Products.ViewModels;
  using TestSA2.Web.Mvc.Extensions;

  [Transaction]
  public class ProductsController : Controller
  {
    private const int DefaultPageSize = 20;

    private readonly ICommandProcessor commandProcessor;

    private readonly IProductQueries productQueries;

    private readonly IProductTasks productTasks;

    public ProductsController(ICommandProcessor commandProcessor, IProductQueries productQueries, IProductTasks productTasks)
    {
      this.commandProcessor = commandProcessor;
      this.productQueries = productQueries;
      this.productTasks = productTasks;
    }

    [HttpGet]
    [ModelStateToTempData]
    public ActionResult Create()
    {
      var viewModel = TempData.SafeGet<ProductViewModel>() ?? new ProductViewModel();
      return this.View(viewModel);
    }

    [HttpPost]
    [ModelStateToTempData]
    public ActionResult Create(ProductViewModel viewModel)
    {
      if (!ViewData.ModelState.IsValid)
      {
        TempData.SafeAdd(viewModel);
        return this.RedirectToAction(c => c.Create());
      }

      try
      {
        var command = new CreateProductCommand(viewModel.Name);
        commandProcessor.Process(command);

        return this.RedirectToAction("Index");
      }
      catch
      {
        return this.View();
      }
    }

    [HttpPost]
    public ActionResult Delete(int id)
    {
      try
      {
        this.productTasks.Delete(id);
        return this.RedirectToAction("Index");
      }
      catch 
      {
        return this.RedirectToAction("Index");
      }
    }

    public ActionResult Details(int id)
    {
      var viewModel = this.productQueries.GetViewModel(id);
      return this.View(viewModel);
    }

    public ActionResult Edit(int id)
    {
      var viewModel = this.productQueries.GetViewModel(id);
      return this.View(viewModel);
    }

    [HttpPost]
    public ActionResult Edit(ProductViewModel viewModel)
    {
      if (!ViewData.ModelState.IsValid) 
      {
        return this.RedirectToAction(c => c.Edit(viewModel.Id));
      }

      try
      {
        this.productTasks.Update(viewModel.Id, viewModel.Name);
        return this.RedirectToAction("Index");
      }
      catch
      {
        return this.View();
      }
    }

    public ActionResult Index(int? page)
    {
      var viewModel = this.productQueries.GetPagedList(page ?? 1, DefaultPageSize);
      return this.View(viewModel);
    }

    [HttpGet]
    public ActionResult Upload()
    {
      return this.View();
    }

    [HttpPost]
    public ActionResult Upload(FormCollection formCollection)
    {
      var uploadsFolder = HostingEnvironment.MapPath("~/App_Data");
      if (string.IsNullOrEmpty(uploadsFolder)) {
        return this.RedirectToAction("Index");
      }

      var files = new List<FileInfo>();
      foreach (string fileName in this.Request.Files) {
        var file = Request.Files[fileName];
        if (file == null || file.ContentLength == 0) {
          continue;
        }

        var path = Path.Combine(uploadsFolder, file.FileName);
        file.SaveAs(path);
        files.Add(new FileInfo
          {
            name = file.FileName, 
            size = file.ContentLength,
            url = "/Products/view/" + file.FileName,
            delete_url = "/Products/delete/" + file.FileName,
          });
      }

      return Json(files);
    }

    public class FileInfo
    {
      public string name { get; set; }

      public int size { get; set; }

      public string url { get; set; }

      public string delete_url { get; set; }
    }
  }
}