using System.Collections;

namespace ReportGenerator.Utils
{
    /// <summary>
    /// Упрощенная реализация двусвязного списка
    /// </summary>
    /// <typeparam name="T">Тип элементов списка</typeparam>
    public class List<T> : ICollection<T>, IEnumerable<T>
    {
        /// <summary>
        /// Узел списка
        /// </summary>
        private class Node
        {
            public T Value { get; set; }
            public Node? Next { get; set; }
            public Node? Previous { get; set; }

            public Node(T value)
            {
                Value = value;
            }
        }

        private Node? _head;
        private Node? _tail;
        private int _count;
        private int _version;

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public List()
        {
            _head = null;
            _tail = null;
            _count = 0;
        }

        /// <summary>
        /// Конструктор из коллекции
        /// </summary>
        public List(IEnumerable<T> collection) : this()
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var item in collection)
            {
                AddLast(item);
            }
        }

        /// <summary>
        /// Количество элементов в списке
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Является ли коллекция только для чтения
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Первый элемент списка
        /// </summary>
        public T First => _head != null ? _head.Value : throw new InvalidOperationException("Список пуст");

        /// <summary>
        /// Последний элемент списка
        /// </summary>
        public T Last => _tail != null ? _tail.Value : throw new InvalidOperationException("Список пуст");

        /// <summary>
        /// Добавляет элемент в конец списка
        /// </summary>
        public void Add(T item)
        {
            AddLast(item);
        }

        /// <summary>
        /// Добавляет элемент в начало списка
        /// </summary>
        public void AddFirst(T item)
        {
            var newNode = new Node(item);

            if (_head == null)
            {
                _head = _tail = newNode;
            }
            else
            {
                newNode.Next = _head;
                _head.Previous = newNode;
                _head = newNode;
            }

            _count++;
            _version++;
        }

        /// <summary>
        /// Добавляет элемент в конец списка
        /// </summary>
        public void AddLast(T item)
        {
            var newNode = new Node(item);

            if (_tail == null)
            {
                _head = _tail = newNode;
            }
            else
            {
                newNode.Previous = _tail;
                _tail.Next = newNode;
                _tail = newNode;
            }

            _count++;
            _version++;
        }

        /// <summary>
        /// Очищает список
        /// </summary>
        public void Clear()
        {
            _head = null;
            _tail = null;
            _count = 0;
            _version++;
        }

        /// <summary>
        /// Проверяет наличие элемента
        /// </summary>
        public bool Contains(T item)
        {
            return Find(item) != null;
        }

        /// <summary>
        /// Копирует элементы в массив
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < _count)
                throw new ArgumentException("Недостаточно места в целевом массиве");

            var current = _head;
            int index = arrayIndex;

            while (current != null)
            {
                array[index++] = current.Value;
                current = current.Next;
            }
        }

        /// <summary>
        /// Удаляет первое вхождение элемента
        /// </summary>
        public bool Remove(T item)
        {
            var nodeToRemove = Find(item);
            if (nodeToRemove != null)
            {
                RemoveNode(nodeToRemove);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Удаляет первый элемент
        /// </summary>
        public void RemoveFirst()
        {
            if (_head == null)
                throw new InvalidOperationException("Список пуст");

            RemoveNode(_head);
        }

        /// <summary>
        /// Удаляет последний элемент
        /// </summary>
        public void RemoveLast()
        {
            if (_tail == null)
                throw new InvalidOperationException("Список пуст");

            RemoveNode(_tail);
        }

        /// <summary>
        /// Возвращает перечислитель
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Находит узел с указанным значением
        /// </summary>
        private Node? Find(T value)
        {
            var current = _head;
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            while (current != null)
            {
                if (comparer.Equals(current.Value, value))
                {
                    return current;
                }
                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// Удаляет узел из списка
        /// </summary>
        private void RemoveNode(Node node)
        {
            if (node.Previous != null)
            {
                node.Previous.Next = node.Next;
            }
            else
            {
                _head = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Previous = node.Previous;
            }
            else
            {
                _tail = node.Previous;
            }

            _count--;
            _version++;
        }

        /// <summary>
        /// Перечислитель для List
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly List<T> _list;
            private Node? _current;
            private int _version;

            internal Enumerator(List<T> list)
            {
                _list = list;
                _current = null;
                _version = list._version;
            }

            public T Current => _current != null ? _current.Value : throw new InvalidOperationException();

            object? IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException("Коллекция была изменена во время перечисления");

                if (_current == null)
                {
                    _current = _list._head;
                }
                else
                {
                    _current = _current.Next;
                }

                return _current != null;
            }

            public void Reset()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException("Коллекция была изменена во время перечисления");

                _current = null;
            }
        }

        // Дополнительные методы

        /// <summary>
        /// Проверяет, пуст ли список
        /// </summary>
        public bool IsEmpty() => _count == 0;

        /// <summary>
        /// Обменивает содержимое с другим списком
        /// </summary>
        public void Swap(List<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            (_head, other._head) = (other._head, _head);
            (_tail, other._tail) = (other._tail, _tail);
            (_count, other._count) = (other._count, _count);
            (_version, other._version) = (other._version, _version);
        }
    }
}
