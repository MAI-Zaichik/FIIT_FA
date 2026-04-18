using TreeDataStructures.Core;
using System.Diagnostics.CodeAnalysis;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey> // ключ должен быть сравнимым
{
    // передача компаратора в базовый класс
    public AvlTree(IComparer<TKey>? comparer = null) : base(comparer)
    {
    }

    public AvlTree() : this(null) { }
    
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    private static int GetHeight(AvlNode<TKey, TValue>? node)
        => node?.Height ?? 0;
    
    // обновление высоты узла
    private static void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        if (node != null)
        {
            node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
        }
    }
    
    // получение фактора баланса, должен быть от -1 до 1
    private static int GetBalanceFactor(AvlNode<TKey, TValue>? node)
        => GetHeight(node?.Left) - GetHeight(node?.Right);

    // балансируем дерево, используя повороты из базового класса   
    private AvlNode<TKey, TValue>? Balance(AvlNode<TKey, TValue>? node)
    {
        if (node == null) return null;
        
        UpdateHeight(node);
        
        int balance = GetBalanceFactor(node);
        
        // Левое поддерево тяжелее
        if (balance > 1)
        {
            // LR случай
            if (GetBalanceFactor(node.Left) < 0)
            {
                RotateDoubleRight(node);
            }
            // LL случай
            else
            {
                RotateRight(node);
            }
        }
        // Правое поддерево тяжелее
        else if (balance < -1)
        {
            // RL случай
            if (GetBalanceFactor(node.Right) > 0)
            {
                RotateDoubleLeft(node);
            }
            // RR случай
            else
            {
                RotateLeft(node);
            }
        }
        
        return node;
    }
    
    //Балансировка дерева после удаления или вставки
    private void BalanceUpwards(AvlNode<TKey, TValue>? startNode)
    {
        var current = startNode;
        while (current != null)
        {
            var parent = current.Parent;
            
            // Обновляем высоты детей перед балансировкой текущего
            if (current.Left != null)
                UpdateHeight(current.Left);
            if (current.Right != null)
                UpdateHeight(current.Right);
            
            Balance(current);
            
            current = parent;
        }
        
        // Обновляем корень, если нужно
        if (Root != null)
            UpdateHeight(Root);
    }
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        BalanceUpwards(newNode.Parent);
    }
    
    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        BalanceUpwards(parent);
    }
}