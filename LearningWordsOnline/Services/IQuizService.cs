using Microsoft.AspNetCore.SignalR;
using LearningWordsOnline.Hubs;
using LearningWordsOnline.Models;
using System.Collections.Concurrent;
using LearningWordsOnline.GameLogic;
using LearningWordsOnline.Data;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace LearningWordsOnline.Services
{
    public interface IQuizService
    {
        /// <summary>
        /// 引数に応じたランダムな問題リストを生成する。
        /// </summary>
        /// <param name="questionCount">問題数</param>
        /// <param name="language">どの言語か</param>
        /// <param name="categoryId">カテゴリの指定(子カテゴリがあるにもかかわらず親カテゴリを指定すると問題が作られない</param>
        /// <param name="level">WordのLevelを指定 (Weblio参考)</param>
        /// <param name="appUserId">appUserIdの苦手問題を出題します</param>
        /// <returns>問題リスト</returns>
        Task<IList<Question>> GenerateRandomQuestions(int questionCount, Language language, int? categoryId = null, int? level = null, int? appUserId = null);
    }
}
