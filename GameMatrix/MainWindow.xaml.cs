using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GameMatrix
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int rows = 2;
        private int columns = 2;
        private TextBox[,] matrixTextBoxes;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ManualFill_Click(object sender, RoutedEventArgs e)
        {
            if (!ParseMatrixDimensions()) return;
            CreateMatrixGrid();
        }

        private void RandomFill_Click(object sender, RoutedEventArgs e)
        {
            if (!ParseMatrixDimensions()) return;
            CreateMatrixGrid();

            Random rand = new Random();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    matrixTextBoxes[i, j].Text = rand.Next(2, 11).ToString();
                }
            }
        }

        private bool ParseMatrixDimensions()
        {
            if (int.TryParse(RowsInput.Text, out int m) && int.TryParse(ColumnsInput.Text, out int n)
                && m >= 2 && m <= 20 && n >= 2 && n <= 20)
            {
                rows = m;
                columns = n;
                return true;
            }
            else
            {
                MessageBox.Show("Введите корректные размеры матрицы (от 2 до 20).");
                return false;
            }
        }

        private void CreateMatrixGrid()
        {
            MatrixGrid.Children.Clear();
            MatrixGrid.Rows = rows;
            MatrixGrid.Columns = columns;
            matrixTextBoxes = new TextBox[rows, columns];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    TextBox textBox = new TextBox
                    {
                        Width = 50,
                        Height = 30,
                        Margin = new Thickness(2),
                        TextAlignment = TextAlignment.Center
                    };
                    matrixTextBoxes[i, j] = textBox;
                    MatrixGrid.Children.Add(textBox);
                }
            }
        }

        private int[,] GetMatrixValues()
        {
            int[,] matrix = new int[rows, columns];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    matrix[i, j] = int.Parse(matrixTextBoxes[i, j].Text);
                }
            }
            return matrix;
        }

        private void SolveGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int[,] matrix = GetMatrixValues();
                int minimax = CalculateMinimax(matrix);
                int maximin = CalculateMaximin(matrix);
                string result = $"Нижняя цена игры (минимакс): {minimax}\nВерхняя цена игры (максимин): {maximin}\n";

                // Поиск всех седловых точек
                var saddlePoints = FindSaddlePoints(matrix);

                if (saddlePoints.Count > 0)
                {
                    foreach (var point in saddlePoints)
                    {
                        int row = point.Item1 + 1;  // строка, индекс +1 для отображения с 1
                        int col = point.Item2 + 1;  // столбец, индекс +1 для отображения с 1
                        result += $"Седловая точка на пересечении строки {row} и столбца {col}\n";
                    }
                    result += "\nЧистая стратегия игры:\n";
                    result += "Игрок A должен выбрать строки:\n";
                    foreach (var point in saddlePoints)
                    {
                        result += $"Строка {point.Item1 + 1}\n";
                    }
                    result += "Игрок B должен выбрать столбцы:\n";
                    foreach (var point in saddlePoints)
                    {
                        result += $"Столбец {point.Item2 + 1}\n";
                    }
                }
                else
                {
                    result += "Седловых точек нет.\n";

                    if (rows > 2 || columns > 2)
                    {
                        result += "Пытаемся упростить матрицу...\n";
                        matrix = ReduceMatrix(matrix);  // Упрощаем матрицу
                    }

                    if (rows == 2 && columns == 2)
                    {
                        // Решение игры 2x2 в смешанных стратегиях
                        result += SolveTwoByTwoMatrix(matrix);
                    }
                    else
                    {
                        result += "Невозможно упростить матрицу до 2x2. Решение невозможно.\n";
                    }
                }

                GameResults.Text = result;
            }
            catch (FormatException)
            {
                MessageBox.Show("Пожалуйста, введите все элементы матрицы корректно.");
            }
        }

        // Метод для поиска седловых точек
        private List<Tuple<int, int>> FindSaddlePoints(int[,] matrix)
        {
            var saddlePoints = new List<Tuple<int, int>>();
            int[] rowMins = new int[rows];
            int[] colMaxs = new int[columns];

            // Находим минимумы по строкам
            for (int i = 0; i < rows; i++)
            {
                int rowMin = int.MaxValue;
                for (int j = 0; j < columns; j++)
                {
                    if (matrix[i, j] < rowMin)
                        rowMin = matrix[i, j];
                }
                rowMins[i] = rowMin;
            }

            // Находим максимумы по столбцам
            for (int j = 0; j < columns; j++)
            {
                int colMax = int.MinValue;
                for (int i = 0; i < rows; i++)
                {
                    if (matrix[i, j] > colMax)
                        colMax = matrix[i, j];
                }
                colMaxs[j] = colMax;
            }

            // Ищем седловые точки на пересечении минимумов строк и максимумов столбцов
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (matrix[i, j] == rowMins[i] && matrix[i, j] == colMaxs[j])
                    {
                        saddlePoints.Add(new Tuple<int, int>(i, j));  // добавляем позицию седловой точки
                    }
                }
            }

            return saddlePoints;
        }

        // Метод для упрощения матрицы
        private int[,] ReduceMatrix(int[,] matrix)
        {
            bool[] rowDominated = new bool[rows];
            bool[] colDominated = new bool[columns];

            // Ищем доминируемые строки
            for (int i = 0; i < rows; i++)
            {
                for (int k = 0; k < rows; k++)
                {
                    if (i != k && !rowDominated[k] && IsRowDominated(matrix, i, k))
                    {
                        rowDominated[k] = true; // Строка k доминируется строкой i
                    }
                }
            }

            // Ищем доминируемые столбцы
            for (int j = 0; j < columns; j++)
            {
                for (int l = 0; l < columns; l++)
                {
                    if (j != l && !colDominated[l] && IsColumnDominated(matrix, j, l))
                    {
                        colDominated[l] = true; // Столбец l доминируется столбцом j
                    }
                }
            }

            // Формируем новую матрицу без доминируемых строк и столбцов
            int newRows = rows - rowDominated.Count(x => x);
            int newColumns = columns - colDominated.Count(x => x);
            int[,] reducedMatrix = new int[newRows, newColumns];

            int newRow = 0;
            for (int i = 0; i < rows; i++)
            {
                if (rowDominated[i]) continue; // Пропускаем доминируемые строки

                int newCol = 0;
                for (int j = 0; j < columns; j++)
                {
                    if (colDominated[j]) continue; // Пропускаем доминируемые столбцы

                    reducedMatrix[newRow, newCol] = matrix[i, j];
                    newCol++;
                }
                newRow++;
            }

            // Обновляем размеры
            rows = newRows;
            columns = newColumns;

            return reducedMatrix;
        }

        // Проверка доминирования строки
        private bool IsRowDominated(int[,] matrix, int i, int k)
        {
            bool strictlyLess = false;
            for (int j = 0; j < columns; j++)
            {
                if (matrix[i, j] > matrix[k, j])
                    return false; // Если хотя бы один элемент в строке i больше соответствующего элемента в строке k
                if (matrix[i, j] < matrix[k, j])
                    strictlyLess = true; // Если хотя бы один элемент в строке i меньше соответствующего элемента в строке k
            }
            return strictlyLess;
        }

        // Проверка доминирования столбца
        private bool IsColumnDominated(int[,] matrix, int j, int l)
        {
            bool strictlyLess = false;
            for (int i = 0; i < rows; i++)
            {
                if (matrix[i, j] > matrix[i, l])
                    return false; // Если хотя бы один элемент в столбце j больше соответствующего элемента в столбце l
                if (matrix[i, j] < matrix[i, l])
                    strictlyLess = true; // Если хотя бы один элемент в столбце j меньше соответствующего элемента в столбце l
            }
            return strictlyLess;
        }

        private string SolveTwoByTwoMatrix(int[,] matrix)
        {
            // Пример вычисления вероятностей для смешанных стратегий
            int a = matrix[0, 0], b = matrix[0, 1], c = matrix[1, 0], d = matrix[1, 1];
            double p = (double)(d - b) / (a - b - c + d);
            double q = (double)(d - c) / (a - b - c + d);
            return $"Игра решена в смешанных стратегиях:\nИгрок A: p = {p:F2}, 1-p = {(1 - p):F2}\nИгрок B: q = {q:F2}, 1-q = {(1 - q):F2}\n";
        }

        private int CalculateMinimax(int[,] matrix)
        {
            int minimax = int.MaxValue;
            for (int j = 0; j < columns; j++)
            {
                int colMax = int.MinValue;
                for (int i = 0; i < rows; i++)
                {
                    if (matrix[i, j] > colMax)
                        colMax = matrix[i, j];
                }
                if (colMax < minimax)
                    minimax = colMax;
            }
            return minimax;
        }

        private int CalculateMaximin(int[,] matrix)
        {
            int maximin = int.MinValue;
            for (int i = 0; i < rows; i++)
            {
                int rowMin = int.MaxValue;
                for (int j = 0; j < columns; j++)
                {
                    if (matrix[i, j] < rowMin)
                        rowMin = matrix[i, j];
                }
                if (rowMin > maximin)
                    maximin = rowMin;
            }
            return maximin;
        }
    }
}