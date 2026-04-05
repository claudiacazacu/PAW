# Lessons Learned 

## De ce folosim Repository Pattern?
Folosim Repository Pattern pentru a crea o barieră de izolare între baza de date și restul aplicației. Acesta tratează baza de date ca pe o simplă colecție de obiecte, eliminând nevoia de a scrie interogări Entity Framework prin toate componentele. Acest lucru face codul mult mai curat, ușor de întreținut și ne permite să modificăm structura bazei de date într-un singur loc fără să afectăm logica de business sau interfața.

## Ce s-ar întâmpla dacă apelăm _context direct din controller?
Dacă am folosi `_context` direct în controller, am crea o dependență prea strânsă (tight coupling) între baza de date și interfața utilizatorului. Controller-ul ar deveni supraîncărcat, ocupându-se simultan de cereri HTTP, validări și interogări SQL. Această abordare face testarea unitară aproape imposibilă fără o bază de date reală și transformă orice modificare minoră în tabelele bazei de date într-un risc de erori în lanț în tot proiectul.

## De ce avem un Service Layer separat și ce logică ar ajunge în controller fără el?
Service Layer-ul este locul unde stă întreaga logică de business. Avem nevoie de el pentru a păstra controller-ele subțiri și concentrate strict pe gestionarea cererilor utilizatorului. Fără acest strat, toată logica — cum ar fi setarea automată a datei de publicare, procesarea imaginilor sau validările complexe între mai multe tabele — ar ajunge în controller. Acesta ar deveni greu de citit, greu de testat și imposibil de reutilizat în alte părți ale aplicației.

## De ce folosim interfețe (IArticleRepository, IArticleService)?
Interfețele ne permit să folosim Dependency Injection, ceea ce face aplicația extrem de flexibilă și modulară. În loc să depindem de clase concrete, depindem de niște contracte. Acest lucru ne permite să schimbăm oricând implementarea unui serviciu fără să modificăm codul care îl apelează și ne oferă posibilitatea de a injecta obiecte de tip Mock în timpul testării, pentru a verifica componentele în izolare totală.

## Cum ajută această structură pentru un API REST sau o aplicație mobilă?
Această arhitectură stratificată este ideală pentru scalabilitate. Dacă decidem să adăugăm un API REST sau să dezvoltăm o aplicație mobilă, nu trebuie să rescriem nimic din logica de business sau din accesul la date. Vom crea doar controllere noi care vor returna JSON în loc de pagini HTML, dar care vor folosi exact aceleași servicii și repository-uri pe care le avem deja. Logica rămâne centralizată pe server, garantând că aceleași reguli se aplică indiferent de platforma de pe care sunt accesate datele.

## Dați un exemplu concret din cod.

Un exemplu concret e folosirea interfețelor `IArticleRepository` și `IArticleService` pentru Dependency Injection. Iată interfața repository-ului:

```csharp
public interface IArticleRepository : IRepository<Article>
{
    Task<List<Article>> GetAllWithDetailsAsync();
    Task<Article?> GetByIdWithDetailsAsync(int id);
    Task<List<Article>> GetByCategoryAsync(int categoryId);
    Task<int> CountAsync();
    Task<List<Article>> GetPagedAsync(int page, int pageSize);
}
```

Implementarea în `ArticleRepository`:

```csharp
public class ArticleRepository : Repository<Article>, IArticleRepository
{
    public ArticleRepository(AppDbContext context) : base(context) { }

    public async Task<List<Article>> GetAllWithDetailsAsync()
    {
        return await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.User)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }
    // ... alte metode
}
```

În `ArticleService`, injectăm `IUnitOfWork`:

```csharp
public class ArticleService : IArticleService
{
    private readonly IUnitOfWork _unitOfWork;

    public ArticleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task AddAsync(Article article)
    {
        article.PublishedAt = DateTime.Now; // logică de business
        await _unitOfWork.ArticleRepository.AddAsync(article);
        await _unitOfWork.SaveChangesAsync();
    }
    // ... alte metode
}
```

În `ArticlesController`, injectăm serviciile:

```csharp
public class ArticlesController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;

    public ArticlesController(
        IArticleService articleService,
        ICategoryService categoryService
    )
    {
        _articleService = articleService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Create()
    {
        var categories = await _categoryService.GetAllAsync();
        ViewData["CategoryId"] = new SelectList(categories, "Id", "Name");
        return View();
    }
    // ... alte acțiuni
}
```

Asta permite teste ușoare cu mock-uri și separare clară a responsabilităților.

## Cum ajută această structură pentru un API REST sau o aplicație mobilă?
Această arhitectură stratificată este ideală pentru scalabilitate. Dacă decidem să adăugăm un API REST sau să dezvoltăm o aplicație mobilă, nu trebuie să rescriem nimic din logica de business sau din accesul la date. Vom crea doar controllere noi care vor returna JSON în loc de pagini HTML, dar care vor folosi exact aceleași servicii și repository-uri pe care le avem deja. Logica rămâne centralizată pe server, garantând că aceleași reguli se aplică indiferent de platforma de pe care sunt accesate datele.

---

## Demo YouTube

[Vizioneaza demo-ul pe YouTube](https://youtu.be/KLxxIs5rc6U)