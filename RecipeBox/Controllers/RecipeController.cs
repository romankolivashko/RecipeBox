using Microsoft.AspNetCore.Mvc;
using RecipeBox.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

//new using directives
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Security.Claims;

//////////////////////////////////////////////////////
//////// Authorizing Create, Update and Delete of RecipesController.cs 
//////////////////////////////////////////////////////
//////// 1. Recipe.cs needs ApplicationUser property
//////// 2. RecipesController.cs has various updates
//////// 3. Views/Recipes/Details.cshtml has updates
//////// 4. Views/Recipes/Index.cshtml has updates
//////////////////////////////////////////////////////

namespace RecipeBox.Controllers
{
  public class RecipesController : Controller
  {
    private readonly RecipeBoxContext _db;
    private readonly UserManager<ApplicationUser> _userManager; 

    public RecipesController(UserManager<ApplicationUser> userManager, RecipeBoxContext db)
    {
      _userManager = userManager;
      _db = db;
    }

    //Index Route updated to find all DB items
    public ActionResult Index()
    {
      List<Recipe> userRecipes = _db.Recipes.ToList();
      return View(userRecipes);
    }

    //Create Route updated to Add Authorization
    [Authorize] 
    public ActionResult Create()
    {
      ViewBag.CategoryId = new SelectList(_db.Categories, "CategoryId", "Name");
      return View();
    }
    
    [HttpPost]
    public async Task<ActionResult> Create(Recipe item, int CategoryId)
    {
      var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var currentUser = await _userManager.FindByIdAsync(userId);
      item.User = currentUser;
      _db.Recipes.Add(item);
      _db.SaveChanges();
      if (CategoryId != 0)
      {
        _db.CategoryRecipe.Add(new CategoryRecipe() { CategoryId = CategoryId, RecipeId = item.RecipeId });
      }
      _db.SaveChanges();
      return RedirectToAction("Index");
    }

    // In the Details route we need to find the user associated with the item so that in the view, we can show the edit, delete or add category links if the item "belongs" to that user. Line 93 involves checking if the userId is null: if it is null then IsCurrentUser is set to false, if is not null, then the program evaluates whether userId is equal to thisRecipe.User.Id and returns true if so, false if not so.
    // Line 93 can be refactored into an if statement like so:
    // if (userId != null) 
    // {
    //   if (userId == thisRecipe.User.Id) 
    //   {
    //     ViewBag.IsCurrentUser = true;
    //   }
    //   else
    //   {
    //     ViewBag.IsCurrentUser = false;
    //   }
    // }
    // else 
    // {
    //   ViewBag.IsCurrentUser = false;
    // }
    // Look at the Details view and how IsCurrentUser is used to help comprehend what is happening in the conditional using the ternary operator 
    public ActionResult Details(int id)
    {
      var thisRecipe = _db.Recipes
          .Include(item => item.JoinEntities)
          .ThenInclude(join => join.Category)
          .Include(item => item.User)
          .FirstOrDefault(item => item.RecipeId == id);
      var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      ViewBag.IsCurrentUser = userId != null ? userId == thisRecipe.User.Id : false;
      return View(thisRecipe);
    }

    // Edit Route is updated to find the user and the item that matches the user id, then is routed based on that result. 
    [Authorize]
    public async Task<ActionResult> Edit(int id)
    {
      var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var currentUser = await _userManager.FindByIdAsync(userId);
      var thisRecipe = _db.Recipes
          .Where(entry => entry.User.Id == currentUser.Id)
          .FirstOrDefault(item => item.RecipeId == id);
      if (thisRecipe == null)
      {
        return RedirectToAction("Details", new {id = id});
      }
      ViewBag.CategoryId = new SelectList(_db.Categories, "CategoryId", "Name"); 
      return View(thisRecipe);
    }


    [HttpPost]
    public ActionResult Edit(Recipe item, int CategoryId)
    {
      if (CategoryId != 0)
      {
        _db.CategoryRecipe.Add(new CategoryRecipe() { CategoryId = CategoryId, RecipeId = item.RecipeId });
      }
      _db.Entry(item).State = EntityState.Modified;
      _db.SaveChanges();
      return RedirectToAction("Index");
    }

    // AddCategory is updated to find the user and the item that matches the user id, then is routed based on that result. 
    [Authorize]
    public async Task<ActionResult> AddCategory(int id)
    {
      var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var currentUser = await _userManager.FindByIdAsync(userId);
      Recipe thisRecipe = _db.Recipes
          .Where(entry => entry.User.Id == currentUser.Id)
          .FirstOrDefault(item => item.RecipeId == id);
      if (thisRecipe == null)
      {
        return RedirectToAction("Details", new {id = id});
      }
      ViewBag.CategoryId = new SelectList(_db.Categories, "CategoryId", "Name");
      return View(thisRecipe);
    }

    [HttpPost]
    public ActionResult AddCategory(Recipe item, int CategoryId)
    {
      if (CategoryId != 0)
      {
        _db.CategoryRecipe.Add(new CategoryRecipe() { CategoryId = CategoryId, RecipeId = item.RecipeId });
      }
      _db.SaveChanges();
      return RedirectToAction("Index");
    }

    // Delete route is updated to find the user and the item that matches the user id, then is routed based on that result. 
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
      var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var currentUser = await _userManager.FindByIdAsync(userId);

      Recipe thisRecipe = _db.Recipes
          .Where(entry => entry.User.Id == currentUser.Id)
          .FirstOrDefault(item => item.RecipeId == id);
      if (thisRecipe == null)
      {
        return RedirectToAction("Details", new {id = id});
      }
      return View(thisRecipe);
    }

    [HttpPost, ActionName("Delete")]
    public ActionResult DeleteConfirmed(int id)
    {
      var thisRecipe = _db.Recipes.FirstOrDefault(item => item.RecipeId == id);
      _db.Recipes.Remove(thisRecipe);
      _db.SaveChanges();
      return RedirectToAction("Index");
    }

    [HttpPost]
    public ActionResult DeleteCategory(int joinId)
    {
      var joinEntry = _db.CategoryRecipe.FirstOrDefault(entry => entry.CategoryRecipeId == joinId);
      _db.CategoryRecipe.Remove(joinEntry);
      _db.SaveChanges();
      return RedirectToAction("Index");
    }
  }
}