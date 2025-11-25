using System.Collections;

namespace ReportGenerator.Utils
{
    /// <summary>
    /// Динамический массив с автоматическим изменением размера (аналог std::vector из C++)
    /// </summary>
    /// <typeparam name="T">Тип элементов вектора</typeparam>
    public class Vector<T> : IList<T>, IReadOnlyList<T>
    {
        private T[] _items;
        private int _size;
        private int _version;

        private const int DefaultCapacity = 4;
        private static readonly T[] EmptyArray = new T[0];

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public Vector()
        {
            _items = EmptyArray;
            _size = 0;
        }

        /// <summary>
        /// Конструктор с начальной емкостью
        /// </summary>
        /// <param name="capacity">Начальная емкость</param>
        public Vector(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Емкость не может быть отрицательной");

            _items = capacity == 0 ? EmptyArray : new T[capacity];
            _size = 0;
        }

        /// <summary>
        /// Конструктор из коллекции
        /// </summary>
        /// <param name="collection">Исходная коллекция</param>
        public Vector(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count == 0)
                {
                    _items = EmptyArray;
                }
                else
                {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _items = EmptyArray;
                _size = 0;
                foreach (var item in collection)
                {
                    Add(item);
                }
            }
        }

        /// <summary>
        /// Количество элементов в векторе
        /// </summary>
        public int Count => _size;

        /// <summary>
        /// Емкость внутреннего массива
        /// </summary>
        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < _size)
                    throw new ArgumentOutOfRangeException(nameof(value), "Емкость не может быть меньше текущего размера");

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, newItems, _size);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        _items = EmptyArray;
                    }
                }
            }
        }

        /// <summary>
        /// Является ли коллекция только для чтения
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Индексатор
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _size)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _items[index];
            }
            set
            {
                if (index < 0 || index >= _size)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _items[index] = value;
                _version++;
            }
        }

        /// <summary>
        /// Добавляет элемент в конец вектора
        /// </summary>
        public void Add(T item)
        {
            if (_size == _items.Length)
            {
                EnsureCapacity(_size + 1);
            }
            _items[_size++] = item;
            _version++;
        }

        /// <summary>
        /// Добавляет диапазон элементов
        /// </summary>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_size + count);
                    c.CopyTo(_items, _size);
                    _size += count;
                }
            }
            else
            {
                foreach (var item in collection)
                {
                    Add(item);
                }
            }
            _version++;
        }

        /// <summary>
        /// Очищает вектор
        /// </summary>
        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size);
                _size = 0;
            }
            _version++;
        }

        /// <summary>
        /// Проверяет наличие элемента
        /// </summary>
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        /// <summary>
        /// Копирует элементы в массив
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        /// <summary>
        /// Возвращает индекс элемента
        /// </summary>
        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _size);
        }

        /// <summary>
        /// Вставляет элемент по указанному индексу
        /// </summary>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > _size)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_size == _items.Length)
            {
                EnsureCapacity(_size + 1);
            }

            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }

            _items[index] = item;
            _size++;
            _version++;
        }

        /// <summary>
        /// Удаляет первое вхождение элемента
        /// </summary>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Удаляет элемент по индексу
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index));

            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default!;
            _version++;
        }

        /// <summary>
        /// Удаляет диапазон элементов
        /// </summary>
        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count));

            if (_size - index < count)
                throw new ArgumentException("Недопустимый диапазон");

            if (count > 0)
            {
                _size -= count;
                if (index < _size)
                {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                Array.Clear(_items, _size, count);
                _version++;
            }
        }

        /// <summary>
        /// Изменяет размер вектора
        /// </summary>
        public void Resize(int newSize)
        {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize));

            if (newSize > Capacity)
            {
                EnsureCapacity(newSize);
            }

            if (newSize < _size)
            {
                Array.Clear(_items, newSize, _size - newSize);
            }

            _size = newSize;
            _version++;
        }

        /// <summary>
        /// Уменьшает емкость до текущего размера
        /// </summary>
        public void TrimExcess()
        {
            int threshold = (int)(_items.Length * 0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
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
        /// Обеспечивает достаточную емкость
        /// </summary>
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
                if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        /// <summary>
        /// Перечислитель для Vector
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly Vector<T> _vector;
            private int _index;
            private readonly int _version;
            private T _current;

            internal Enumerator(Vector<T> vector)
            {
                _vector = vector;
                _index = 0;
                _version = vector._version;
                _current = default!;
            }

            public T Current => _current;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _vector._size + 1)
                        throw new InvalidOperationException();
                    return Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _vector._version)
                    throw new InvalidOperationException("Коллекция была изменена во время перечисления");

                if (_index < _vector._size)
                {
                    _current = _vector._items[_index];
                    _index++;
                    return true;
                }

                _index = _vector._size + 1;
                _current = default!;
                return false;
            }

            public void Reset()
            {
                if (_version != _vector._version)
                    throw new InvalidOperationException("Коллекция была изменена во время перечисления");

                _index = 0;
                _current = default!;
            }
        }

        // Дополнительные методы

        /// <summary>
        /// Возвращает последний элемент
        /// </summary>
        public T Back()
        {
            if (_size == 0)
                throw new InvalidOperationException("Вектор пуст");
            return _items[_size - 1];
        }

        /// <summary>
        /// Возвращает первый элемент
        /// </summary>
        public T Front()
        {
            if (_size == 0)
                throw new InvalidOperationException("Вектор пуст");
            return _items[0];
        }

        /// <summary>
        /// Проверяет, пуст ли вектор
        /// </summary>
        public bool Empty() => _size == 0;

        /// <summary>
        /// Зарезервировать емкость
        /// </summary>
        public void Reserve(int capacity)
        {
            if (capacity > Capacity)
            {
                Capacity = capacity;
            }
        }

        /// <summary>
        /// Обменивает содержимое с другим вектором
        /// </summary>
        public void Swap(Vector<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            (_items, other._items) = (other._items, _items);
            (_size, other._size) = (other._size, _size);
            (_version, other._version) = (other._version, _version);
        }
    }
}
