using System;

namespace nPreCredit.Code.Services
{
    public interface IShowInfoOnNextPageLoadService
    {
        void ShowInfoMessageOnNextPageLoad(string title, string text, Uri link = null);
    }
}