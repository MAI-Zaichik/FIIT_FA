using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    // метод для получения списка всех ключей дерева
    public ICollection<TKey> Keys
    {
        get
        {
            var newList = new List<TKey>();
            foreach (var entry in InOrder())
            {
                newList.Add(entry.Key);
            }
            return newList;
        }
    }

    // метод для получения списка всех значений дерева
    public ICollection<TValue> Values
    {
        get
        {
            var newList = new List <TValue>();
            foreach (var entry in InOrder())
            {
                newList.Add(entry.Value);
            }
            return newList;
        }
    }
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode newNode = CreateNode(key, value);
        // если первый узел впринципе
        if (Root == null)
        {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }

        TNode? current = Root;
        TNode? parent = null;
        int cmp = 0;
        // идём до нужного места сравнениями
        while (current != null)
        {
            parent = current;
            cmp = Comparer.Compare(key, current.Key);

            if (cmp == 0)
            {   
                // обновляем значения, если такой ключ уже есть
                current.Value = value;
                return;
            }
            current = cmp < 0 ? current.Left : current.Right;
        }
        // ставим на нужное место, если ключа нет
        newNode.Parent = parent;
        if (cmp < 0)
            parent!.Left = newNode;
        else
            parent!.Right = newNode;

        Count++;
        OnNodeAdded(newNode);
    }

    //вспомогательный метод удаления узла
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        // если у удаляемого узла один потомок
        if (node.Left == null)
        {
            Transplant(node, node.Right);
            OnNodeRemoved(node.Parent, node.Right);
        }
        else if (node.Right == null)
        {
            Transplant(node, node.Left);
            OnNodeRemoved(node.Parent, node.Left);
        }
        else
        {
            //если два потомка
            TNode successor = node.Right;
            // ищем минимальный узел в правом поддереве
            while (successor.Left != null)
            {
                successor = successor.Left;
            }
            // случай, если удаляемый узел не прямой предок successor
            if (successor.Parent != node)
            {
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                if (successor.Right != null)
                    successor.Right.Parent = successor;
            }

            Transplant(node, successor);
            successor.Left = node.Left;
            if (successor.Left != null)
                successor.Left.Parent = successor;

            OnNodeRemoved(node.Parent, successor);
        }
    }

    // проверка существования значения с определённым ключом
    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    // получение значения по ключу
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    // метод позволяет работать с деревом как с массивом
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    // метод поиска узлов по ключу
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    // поворот влево
    protected void RotateLeft(TNode x)
    {
        // у станет новым корнем поддерева
        TNode? y = x.Right;
        if (y == null) return;

        // левое поддерево у станет правым поддеревом х
        x.Right = y.Left;
        if (y.Left != null)
            y.Left.Parent = x;

        y.Parent = x.Parent;

        if (x.Parent == null)
            Root = y; // х был корнем
        else if (x.IsLeftChild)
            x.Parent.Left = y;
        else
            x.Parent.Right = y;

        // х становится левым потомком у
        y.Left = x; 
        x.Parent = y;
    }

    // зеркальное отражение левого поворота
    protected void RotateRight(TNode y)
    {
        TNode? x = y.Left;
        if (x == null) return;

        y.Left = x.Right;
        if (x.Right != null)
            x.Right.Parent = y;

        x.Parent = y.Parent;

        if (y.Parent == null)
            Root = x;
        else if (y.IsLeftChild)
            y.Parent.Left = x;
        else
            y.Parent.Right = x;

        x.Right = y;
        y.Parent = x;
    }
    
    // для авл деревьев, балансировка для дисбаланса справа
    protected void RotateBigLeft(TNode x)
    {
        if (x.Right is null) return;
        RotateRight(x.Right);
        RotateLeft(x);
    }
    
    // зеркально предыдущему
    protected void RotateBigRight(TNode y)
    {
        if (y.Left is null) return;
        RotateLeft(y.Left);
        RotateRight(y);
    }
    
    // для сплау деревьев два левых подряд
    protected void RotateDoubleLeft(TNode x)
    {
        TNode? y = x.Right;
        if (y == null) return;
        RotateLeft(x);
        RotateLeft(y);
    }
    
    // зеркально предыдущему
    protected void RotateDoubleRight(TNode y)
    {
        TNode? x = y.Left;
        if (x == null) return;
        RotateRight(y);
        RotateRight(x);
    }
    
    // метод для замены одного узла на другой
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    //private IEnumerable<TreeEntry<TKey, TValue>>  InOrderTraversal(TNode? node)
    //{
    //    if (node == null) {  yield break; }
    //    throw new NotImplementedException();
    //}
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private class TreeIterator:
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy; // or make it template parameter?
        private TNode? _current; // текущий узел в процессе итерации
        private bool _started; // флаг начала итерации

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _current = null;
            _started = false;
        }
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        // метод получения текущего узла итерации
        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if(_current is null)
                {
                    throw new InvalidOperationException();
                }
                return new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetCurrentDepth(_current));
            }
        }

        // метод получения глубины узла
        private int GetCurrentDepth(TNode? node)
        {
            int depth = 0;
            while (node?.Parent is not null)
            {
                depth++;
                node = node.Parent;
            }
            return depth;
        }
        object IEnumerator.Current => Current;
        
        // метод перехода к следующему узлу
        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                _current = GetFirstNode(_root, _strategy);
                return _current is not null;
            }

            _current = GetNextNode(_current,_strategy);
            return _current is not null;
        }
        
        // сброс итератора
        public void Reset()
        {
            _current = null;
            _started = false;
        }

        // очистка ресурсов
        public void Dispose()
        {
            // TODO release managed resources here
        }

        // поиск первого узла обхода
        private static TNode? GetFirstNode(TNode? root, TraversalStrategy strategy)
        {
            if (root is null) return null;

            return strategy switch
            {
                TraversalStrategy.InOrder => GoLeft(root),
                TraversalStrategy.PreOrder => root,
                TraversalStrategy.PostOrder => GoLeftR(root),
                TraversalStrategy.InOrderReverse => GoRight(root),
                TraversalStrategy.PreOrderReverse => root,
                TraversalStrategy.PostOrderReverse => GoRightL(root),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy))
            };
        }

        // получение следующего узла при очередной итерации
        private static TNode? GetNextNode(TNode? node, TraversalStrategy strategy)
        {
            if (node is null) return null;
            return strategy switch
            {
                TraversalStrategy.InOrder => NextInOrder(node),
                TraversalStrategy.InOrderReverse => NextInOrderReverse(node),
                TraversalStrategy.PreOrder => NextPreOrder(node),
                TraversalStrategy.PreOrderReverse => NextPreOrderReverse(node),
                TraversalStrategy.PostOrder => NextPostOrder(node),
                TraversalStrategy.PostOrderReverse => NextPostOrderReverse(node),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy))
            };
        }

        // до конца влево
        private static TNode GoLeft(TNode node)
        {
           while (node.Left is not null) node = node.Left;
           return node;
        }

        // идти влево с приоритетом левого потомка
        private static TNode GoLeftR(TNode node)
        {
            while (node.Left is not null || node.Right is not null)
            {
                if (node.Left is null)
                {
                    node = node.Right!;
                }
                else
                {
                    node = node.Left!;
                }
            }
            return node;
        }

        // до конца вправо
        private static TNode GoRight(TNode node)
        {
            while(node.Right is not null) node = node.Right;
            return  node;
        }

        // до конца вправо с приоритетом правого потомка
        private static TNode GoRightL(TNode node)
        {
            while (node.Left is not null || node.Right is not null)
            {
                if (node.Right is null)
                {
                    node = node.Left!;
                }
                else
                {
                    node = node.Right!;
                }
            }
            return node;
        }

        // для инфиксного (л - к - п) он же симметричный
        private static TNode? NextInOrder(TNode node)
        {
            if (node.Right is not null)
            {
                return GoLeft(node.Right);
            }
            var current = node;
            while (current.Parent is not null && current.IsRightChild)
            {
                current = current.Parent;
            }
            return current.Parent;
        }

        // реверс предыдущего обхода (п - к - л) 
        private static TNode? NextInOrderReverse(TNode node)
        {
            if (node.Left is not null)
            {
                return GoRight(node.Left);
            }
            var current = node;
            while (current.Parent is not null && current.IsLeftChild)
            {
                current = current.Parent;
            }
            return current.Parent;
        }

        // для префиксного (к - л - п) он же прямой
        private static TNode? NextPreOrder(TNode node)
        {
            if (node.Left is not null) return node.Left;
            if (node.Right is not null) return node.Right;

            var current = node;
            while (current.Parent is not null)
            {
                if (current.Parent.Right is not null && current.IsLeftChild) return current.Parent.Right;
                current = current.Parent;
            }
            return null;
        }

        // реверс предыдущего (к - п - л)
        private static TNode? NextPreOrderReverse(TNode node)
        {
            if (node.Right is not null) return node.Right;
    
            if (node.Left is not null) return node.Left;
    
            var current = node;
            while (current.Parent is not null)
            {
                if (current.Parent.Left is not null && current.IsRightChild)
                    return current.Parent.Left;
                current = current.Parent;
            }
    
            return null;
        }

        // для постфиксного обхода (л - п - к) он же обратный
        private static TNode? NextPostOrder(TNode node)
        {
            if (node.Parent is null) return null;
            if (node.IsLeftChild && node.Parent.Right is not null) return GoLeftR(node.Parent.Right);
            return node.Parent;

        }

        // реверс бредыдущего обхода (п - л - к)
        private static TNode? NextPostOrderReverse(TNode node)
        {
            if (node.Parent is null) return null;
    
            // Если мы правый потомок и у родителя есть левый потомок
            if (node.IsRightChild && node.Parent.Left is not null)
                return GoRightL(node.Parent.Left);
    
            // Иначе поднимаемся к родителю
            return node.Parent;
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    // поддержка foreach, чтобы дерево можно было перебрать
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new DictionaryEnumerator(Root);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // адаптер для словаря
    private class DictionaryEnumerator(TNode? root) : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private TreeIterator _inner = new(root, TraversalStrategy.InOrder);
        public KeyValuePair<TKey, TValue> Current => new(_inner.Current.Key, _inner.Current.Value);
        object IEnumerator.Current => Current;
        public bool MoveNext() => _inner.MoveNext();
        public void Reset() => _inner.Reset();
        public void Dispose() { }
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    // копирование в массив
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Destination is too small", nameof(array));

        foreach (var entry in InOrder())
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}