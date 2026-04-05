namespace Lab06.ViewModels;

public class HomeViewModel
{
    public List<ArticleViewModel> LatestArticles { get; set; } = new();
    public int TotalArticles { get; set; }
    public int TotalCategories { get; set; }
}
