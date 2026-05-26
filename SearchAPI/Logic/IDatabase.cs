using System.Collections.Generic;
using Shared.Model;

namespace SearchAPI.Logic;
    public interface IDatabase
    {
        

        /// <summary>
        /// Get document details by its id
        /// </summary>
        BEDocument GetDocDetails(int docId);

        /// <summary>
        /// Perform the essential search for documents. It will return
        /// a list of pairs - the docId is the id of the
        /// document, and hits is the number of words from the query
        /// contained in the document.
        /// Ordered decending by hits
        /// </summary>
        List<(int docId, int hits)> GetDocuments(List<int> wordIds, int maxAmount, int offset);
        int CountDocuments(List<int> wordIds);

        /// <summary>
        /// Return all words, contained in [wordIds], but not
        /// present in the document with id [docId]
        /// </summary>
        List<string> GetMissing(int docId, List<int> wordIds);
        
        
        /// <summary>
        /// Return all words, contained in [wordIds], and
        /// present in the document with id [docId]
        /// </summary>
        List<string> GetHits(int docId, List<int> wordIds);
        

        /// <summary>
        /// </summary>
        /// <returns>all words - the key is the word itself, and value is the id</returns>
        Dictionary<string, int> GetAllWords();
        string? GetFileContent(int docId);
        
    }
