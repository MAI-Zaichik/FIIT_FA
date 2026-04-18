using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    public Treap() : this(null) { }
    public Treap(IComparer<TKey>? comparer = null) : base(comparer)
    {
        
    }
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);

        int cmp = Comparer.Compare(key, root.Key);

        if (cmp < 0)
        {
            // рекурсивно режем левое поддерево
            var (leftSubtree, rightSubtree) = Split(root.Left, key);
            root.Left = rightSubtree;

            if (rightSubtree != null)
                rightSubtree.Parent = root;
            if (leftSubtree != null)
                leftSubtree.Parent = null;

            root.Parent = null;
            return (leftSubtree, root);
        }
        else
        {
            // рекурсивно режем правое поддерево
            var (leftSubtree, rightSubtree) = Split(root.Right, key);
            root.Right = leftSubtree;

            if (leftSubtree != null)
                leftSubtree.Parent = root;
            if (rightSubtree != null)
                rightSubtree.Parent = null;
            
            root.Parent = null;
            return (root, rightSubtree);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;
        
        // Сравниваем приоритеты (больший приоритет становится корнем)
        if (left.Priority > right.Priority)
        {
            // рекурсивно сливаем правый от левого поддерева и правое поддерево
            var newRight = Merge(left.Right, right);
            left.Right = newRight;
            
            if (newRight != null)
                newRight.Parent = left;
            
            left.Parent = null;
            return left;
        }
        else
        {
            // рекурсивно сливаем левое поддерево и левое от правого поддерева
            var newLeft = Merge(left, right.Left);
            right.Left = newLeft;
            
            if (newLeft != null)
                newLeft.Parent = right;
            
            right.Parent = null;
            return right;
        }
    }
    
    private new TreapNode<TKey, TValue>? FindNode(TKey key)
    {
        var current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) return current;
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    public override void Add(TKey key, TValue value)
    {
        // Проверяем, нет ли уже такого ключа
        var existingNode = FindNode(key);
        if (existingNode != null)
        {
            // Обновляем значение
            existingNode.Value = value;
            return;
        }
        
        // Разрезаем дерево по ключу
        var (left, right) = Split(Root, key);
        
        // Создаем новый узел
        var newNode = CreateNode(key, value);
        
        // Сливаем: левое правое и новый узел
        var mergedLeft = Merge(left, newNode);
        Root = Merge(mergedLeft, right);
        
        Count++;
        OnNodeAdded(newNode);
    }

    public override bool Remove(TKey key)
    {
        // Проверяем, существует ли ключ
        var node = FindNode(key);
        if (node == null) return false;
        
        // Разрезаем дерево на 3 части
        var (left, temp) = Split(Root, key);
        var (mid, right) = Split(temp, key);

        Root = Merge(left, right);
        
        Count--;
        OnNodeRemoved(node.Parent, node.Right ?? node.Left);
        
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
    }
    
}