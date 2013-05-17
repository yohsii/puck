using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace puck.core.Abstract
{
    public interface I_Puck_Repository
    {
        /*
        int CreateImage(Image image);
        int CreatePlaylist(Playlist playlist);
        int CreateFilter(Filter filter);
        int CreateVideo(Video video);
        int CreateCategory(Category category);
        int CreateHomepage(Homepage homepage);
        
        IQueryable<Image> GetImages();
        IQueryable<Playlist> GetPlaylists(string UserName);
        IQueryable<Filter> GetFilters(string UserName);
        IQueryable<Filter> GetFilters();
        IQueryable<YoutubeAccount> GetYoutubeAccountsByUserId(string userId);
        IQueryable<YoutubeAccount> GetYoutubeAccounts();
        IQueryable<Video> GetVideos(string UserName);
        IQueryable<Video> GetVideos();
        IQueryable<Video> GetVideosByCategory(int categoryId);
        IQueryable<Video> GetVideosByFilter(string filterTerm, string UserName);
        IQueryable<Category> GetCategories(string UserName);
        IQueryable<Category> GetCategories();

        Image GetImageById(int id);
        Playlist GetPlaylistById(int id);
        Filter GetFilterById(int id);
        YoutubeAccount GetActiveYoutubeAccountByUserId(string userId);
        YoutubeAccount GetYoutubeAccountById(int Id);
        Video GetVideoById(int videoId);
        Category GetCategoryById(int id);
        Homepage GetHomepage(string UserName);
        
        void EditImage(Image image);
        void EditPlaylist(Playlist playlist);
        void EditFilter(Filter filter);
        void EditVideo(Video video);
        void EditCategory(Category category);
        void EditHomepage(Homepage homepage);
        
        void DeleteImage(int id);
        void DeletePlaylist(int id);
        void DeleteFilter(int id);
        void DeleteVideo(int id);
        void DeleteCategory(int id);
        
        void AssociateAccount(string youtubeUserName, string userId, string token, bool isDefault);

        void CreateQuestion(Question question);
        void CreateAnswer(Answer answer);
        Question GetQuestionByText(string text);
        Question GetQuestionById(int id);
        Answer GetAnswerByText(string text);
        Answer GetAnswerById(int id);
        IQueryable<Answer> GetAnswersByQuestionId(int id);

        void CreateEntry(Entry entry);
        void CreateEntryToAnswer(EntryToAnswer entryToAnswer);
        IQueryable<Entry> GetEntries();
        IQueryable<EntryToAnswer> GetEntryToAnswer();
        void SaveChanges();
        */
    }
}
