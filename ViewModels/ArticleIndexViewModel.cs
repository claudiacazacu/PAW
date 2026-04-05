namespace Lab06.ViewModels;

public class ArticleIndexViewModel
{
    public List<ArticleViewModel> Articles { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
