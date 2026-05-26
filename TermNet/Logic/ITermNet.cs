namespace TermNet.Logic;

public interface ITermNet
{
    string Name { get; }
    List<(string word, double weight)> GetSynonyms(string word);
}
