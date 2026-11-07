namespace Reki.Application.Posts.AI;

public interface IEncodeSerializer<in T>
{
    string Serialize(T item);
}
