using System.ComponentModel;
using System.Globalization;
using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using SimplexMethodBMSTU;

namespace SimplexMethodBMSTU
{
    class SimplexMethod
    {
        public int n { get; private set; } // свободные переменные (столбики)
        public int m { get; private set; } // базисные переменные ( cтроки) l;;'''''';''/> ;;;;; ;  /l90
        public int int_obj { get; private set; }
        decimal[] cols = null!; // cтолбец s
        public decimal[] cols_new = null!; // cтолбец s новый
        decimal[,] matrix = null!; // массив с кэфами
        decimal[] F = null!;
        public string Func = null!; // Определение функции (max/min)
        public decimal[] F_new = null!;
        public string[] basis_variables = null!;
        public string[] free_variables = null!;
        public decimal[,] matrix_new = null!; // массив с кэфами после преобразования
        public SimplexMethod(int n, int m)
        {
            this.n = n;
            this.m = m;
            Make();
        }
        public void Make()
        {
            cols = new decimal[m];
            matrix = new decimal[m, n];
            F = new decimal[n + 1];
            matrix_new = new decimal[m, n];
            F_new = new decimal[n + 1];
            cols_new = new decimal[m];
            basis_variables = new string[m];
            free_variables = new string[n];
        }
        public void Input()
        {
            Console.Write("Выберите к чему стремится функция (max/min): ");
            Func = Console.ReadLine();

            Console.WriteLine("Введите столбец S:");
            for (int i = 0; i < m; i++)
            {
                cols[i] = decimal.Parse(Console.ReadLine()!);
            }
            Console.WriteLine("Введите массив с кэфами:");
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = decimal.Parse(Console.ReadLine()!);
                }
            }
            Console.WriteLine("Введите строку F:");
            for (int i = 0; i < n + 1; i++)
            {
                F[i] = decimal.Parse(Console.ReadLine()!);
            }
        }

        // Вывод получившейся симплекс таблицы
        public void PrintSimplexTab()
        {
            Console.WriteLine();
            Console.WriteLine("Получившаяся симплекс таблица:");
            Console.Write("\t" + "S_i0" + "\t");
            for(int i = 0; i<n; ++i)
            {
                Console.Write(free_variables[i] + "\t");
            }
            Console.WriteLine();
            for(int i = 0; i<m; ++i)
            {
                Console.Write(basis_variables[i] + "\t");
                Console.Write($"{cols_new[i]:F2}" + "\t");
                for(int j = 0; j<n; ++j)
                {
                   Console.Write($"{matrix_new[i,j]:F2}" + "\t");
                }
                Console.WriteLine("");
            }
            Console.Write("F" + "\t");
            for(int i = 0; i<n+1; ++i)
            {
                Console.Write($"{F_new[i]:F2}" + "\t");
            }
            Console.WriteLine($"\n\nF = {F[0]:F2}");
        }

        void InputVariables()
        {
            for(int i = 0; i<n; ++i)
            {
                free_variables[i] = $"x{i+1}";
            }
            for(int i = 0; i<m; ++i)
            {
                basis_variables[i] = $"x{n+i+1}";
            }
        }

        void SwitchVariables(Func<int> find_row, Func<int> find_col)
        {
            int result_row = find_row();
            int result_col = find_col();
            if(result_row == -1 || result_col == -1)
            {
                return;
            }
            string temp_row = basis_variables[result_row];
            basis_variables[result_row] = free_variables[result_col];
            free_variables[result_col] = temp_row;
        }

        void PrintSimplexTabWithVariables()
        {
            Console.Write("\t" + "S_io" + "\t");
            for(int i = 0; i < n; ++i)
            {
                Console.Write(free_variables[i] + "\t");
            }
            Console.WriteLine();
            for(int i = 0; i < m; ++i)
            {
                Console.Write(basis_variables[i] + "\t" + $"{cols[i]:F2}" + "\t");
                for(int j = 0; j < n; ++j)
                {
                    Console.Write($"{matrix[i,j]:F2}" + "\t");
                }
                Console.WriteLine();
            }
            Console.Write("F" + "\t");
            for(int i = 0; i < n+1; ++i)
            {
                Console.Write($"{F[i]:F2}" + "\t");
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        // Поиск разщешающего столбца
        int Find_col()
        {
            for(int j = 0; j<m; ++j)
            {
                if(cols[j] < 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (matrix[j, i] < 0)
                        {
                            return i;
                        }
                    } 
                }

            }
            return -1;
        }

        // Нахождение отношения для поиска разрешающей строки
        int Find_row()
        {
            decimal a;
            decimal b = 10000000000000000000m;
            int c = -1;
            int col_find = Find_col();
            if(col_find == -1)
            {
                return -1;
            }
            for(int i = 0; i<m; ++i)
            {
                if(matrix[i,col_find] != 0)
                {
                    if(cols[i]/matrix[i,col_find] > 0)
                    {
                        a = cols[i]/matrix[i,col_find];
                        if(a < b)
                        {
                            b = a;
                            c = i;
                        }
                    }  
                }

            }
            return c;
        }

        // Преобразование симплекс-таблицы
        void SimplexTab(Func<int> Find_rowfunc, Func<int> Find_colfunc) 
        {
            int Find_row1 = Find_rowfunc();
            int Find_col1 = Find_colfunc();
            matrix_new[Find_row1,Find_col1] = 1/matrix[Find_row1,Find_col1];
            for(int i = 0; i<n; ++i)
            {
                if(i != Find_col1)
                {
                    matrix_new[Find_row1,i] = matrix[Find_row1,i]/matrix[Find_row1,Find_col1];
                }
            }
            for(int i = 0; i<m; ++i)
            {
                if(i != Find_row1)
                {
                    matrix_new[i,Find_col1] = -matrix[i,Find_col1]/matrix[Find_row1,Find_col1];
                }
            }
            for(int i = 0; i<m; ++i)
            {
                if(i != Find_row1)
                {
                    for(int j = 0; j<n; ++j)
                    {
                        if(j != Find_col1)
                        {
                            matrix_new[i,j] = matrix[i,j] - (matrix[Find_row1,j]*matrix[i,Find_col1])/matrix[Find_row1,Find_col1];
                        }
                    }
                }
            }
            cols_new[Find_row1] = cols[Find_row1]/matrix[Find_row1,Find_col1];
            for(int i = 0; i<m; ++i)
            {
                if(i != Find_row1)
                {
                    cols_new[i] = cols[i] - (matrix[i,Find_col1]*cols[Find_row1])/matrix[Find_row1,Find_col1];
                }
            }
            F_new[Find_col1+1] = -F[Find_col1+1]/matrix[Find_row1,Find_col1];
            F_new[0] = F[0] - ((F[Find_col1+1]*cols[Find_row1])/matrix[Find_row1,Find_col1]);
            if(Find_col1 == n - 1)
            {
                int variable = 0;
                for(int i = 1; i<n+1; ++i)
                {
                    if(i != Find_col1+1)
                    {
                        F_new[i] = F[i] - (F[Find_col1+1]*matrix[Find_row1,variable])/matrix[Find_row1,Find_col1];
                        ++variable;
                    }
                }
            }
            else
            {
                int variable = 1;
                for(int i = 1; i<n+1; ++i)
                {
                    if(i != Find_col1+1)
                    {
                        F_new[i] = F[i] - (F[Find_col1+1]*matrix[Find_row1,variable])/matrix[Find_row1,Find_col1];
                        ++variable;
                    }
                }
            }
        }

        // Функция для проверки наличия решения
        int CheckSystem()
        {
            int CountVariable = 0;
            bool IfHasntASolver = true;
            for(int i = 0; i<m; ++i)
            {
                if(cols[i] < 0)
                {
                    for(int j = 0; j<n; ++j)
                    {
                        if(matrix[i,j] < 0)
                        {
                            IfHasntASolver = false;
                            return 4; // Найден отрицательный элемент => разрешающий столбец, находим разрешающую строчку и продолжаем
                            //симплекс-метод
                        }
                    }
                }
                else
                {
                   ++CountVariable; 
                }
            }
            if(CountVariable == m)
            {
                IfHasntASolver = false;
                return 5; // Опорное решение только, если все свободные кэфы >= 0
            // Опорное решение найдено, продолжаем симплекс-метод
            }
            if(IfHasntASolver)
            {
                return 3; // В строке S нет отрицательных элементов => система не имеет решений
            }
            return -1;
        }

        // Функция для поиска нового разрешающего столбца
        int Find_col_after_general_solver()
        {
            for(int i = 1; i<n+1; ++i)
            {
                if(F[i] > 0)
                {
                    return --i;
                }
            }
            return -1;
        }

        // Функция для поиска новой разрешающей строки
        int Find_row_after_general_solver()
        {
            decimal a;
            decimal c = 10000000000000000000m;
            int b = Find_col_after_general_solver();
            if(b == -1)
            {
                return -1;
            }
            int g = -1;
            for(int i = 0; i<m; ++i)
            {
                if (matrix[i,b] != 0)
                {
                    if(cols[i]/matrix[i,b] > 0)
                    {
                        a = cols[i] / matrix[i,b];
                        if(a<c)
                        {
                            c = a;
                            g = i;
                        }
                    }
                }
            }
            return g;
        }

        // Функция для перемешивания массивов, чтобы реализовать цикл while в функции main_func_for_oprimization для применения симплекс-метода
        // пока не будет получено опорное решение
        public void Refuel_mass()
        {
            for(int i = 0; i<m; ++i)
            {
                cols[i] = cols_new[i];
                for(int j = 0; j<n; ++j)
                {
                    matrix[i,j] = matrix_new[i,j];
                }
            }
            for(int i = 0; i<n+1; ++i)
            {
                F[i] = F_new[i];
            }
        }

        void PrintAfterGeneralSolver()
        {
            Console.Write("\t");
            Console.Write("S_i0" + "\t");
            for(int i = 1; i<n+1; ++i)
            {
                Console.Write($"x{i}" + "\t");
            }
            Console.WriteLine();
            for(int i = 0; i<m; ++i)
            {
                Console.Write($"x{n+i+1}" + "\t");
                Console.Write($"{cols[i]:F2}" + "\t");
                for(int j = 0; j<n; ++j)
                {
                    Console.Write($"{matrix[i,j]:F2}" + "\t");
                }
                Console.WriteLine();
            }
            Console.Write("F" + "\t");
            for(int i = 0; i<n+1; ++i)
            {
                Console.Write($"{F[i]:F2}" + "\t");
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        void ShakeFuncForPrint()
        {
            for(int i = 0; i<n+1; ++i)
            {
                F_new[i] = -F_new[i];
            }
        }

        // Главная функция, которая запускает весь процесс нахождения и оптимизации решения целевой функции
        public void main_func_for_oprimization()
        {
            Input();
            PrintAfterGeneralSolver();
            InputVariables();
            int check_system = CheckSystem();
            if(check_system == 3)
            {
                Console.WriteLine("Система не имеет решений");
                Environment.Exit(0);
            }
            if(check_system == 5)
            {
                if(Find_col_after_general_solver() == -1 || Find_row_after_general_solver() == -1)
                {
                    Console.WriteLine("Система не имеет решений");
                    Environment.Exit(0);
                }
                bool IsItTrue = true;
                while(IsItTrue)
                {
                    IsItTrue = false;
                    for(int i = 1; i<n+1; ++i)
                    {
                        if(F[i] > 0)
                        {
                            IsItTrue = true;
                            break;
                        }
                    }
                    if(IsItTrue)
                    {
                        SimplexTab(Find_row_after_general_solver,Find_col_after_general_solver);
                        Refuel_mass();
                        SwitchVariables(Find_row_after_general_solver,Find_col_after_general_solver);
                        PrintSimplexTabWithVariables();
                    }
                }
               
                // Находим оптимальное решение пока не будет в строке функции(кроме столбца свободных членов) всех отрицательных кэфов
            }
            if(check_system == 4)
            {
                bool IsItTrue = true;
                while(IsItTrue)
                {
                    IsItTrue = false;
                    for(int i = 0; i<m; ++i)
                    {
                        if(cols[i] < 0)
                        {
                            IsItTrue = true;
                            break;
                        }
                    }
                    if(IsItTrue)
                    {
                        SimplexTab(Find_row,Find_col);
                        Refuel_mass();
                        SwitchVariables(Find_row,Find_col);
                        PrintSimplexTabWithVariables();
                    }
                }
                if(Find_col_after_general_solver() == -1 || Find_row_after_general_solver() == -1)
                {
                    Console.WriteLine("Система не имеет решений");
                    Environment.Exit(0);
                }
                IsItTrue = true;
                while(IsItTrue)
                {
                    IsItTrue = false;
                    for(int i = 1; i<n+1; ++i)
                    {
                        if(F[i] > 0)
                        {
                            IsItTrue = true;
                            break;
                        }
                    }
                    if(IsItTrue)
                    {
                        SimplexTab(Find_row_after_general_solver,Find_col_after_general_solver);
                        Refuel_mass();
                        SwitchVariables(Find_row_after_general_solver,Find_col_after_general_solver);
                        PrintSimplexTabWithVariables();
                    }
                }
            }
            if(Func == "max")
            {
                ShakeFuncForPrint();
                PrintSimplexTab();
            }
            else if(Func == "min")
            {
                PrintSimplexTab();
            }
        }
}

// Метод Ветвей и границ
class Branch_and_bounds
{
    int n;
    int m;
    bool IsHasSolve;
    string[] basis_variables_branch_and_bounds = null!;
    decimal[,] matrix_branch_and_bounds = null!;
    decimal[] F_branch_and_bounds = null!;
    decimal[] cols_branch_and_bounds = null!;
    string[] basis_variables_branch_and_bounds_new = null!;
    decimal[,] matrix_branch_and_bounds_new = null!;
    decimal[] F_branch_and_bounds_new = null!;
    decimal[] cols_branch_and_bounds_new = null!;
    string[] basis_variables_branch_and_bounds_new_additional = null!;
    decimal[,] matrix_branch_and_bounds_new_additional = null!;
    decimal[] F_branch_and_bounds_new_additional = null!;
    decimal[] cols_branch_and_bounds_new_additional = null!;
    
    public Branch_and_bounds(int n, int m)
    {
        this.n = m;
        this.m = n;
    }

    void Make()
    {
        matrix_branch_and_bounds = new decimal[m+1,n];
        F_branch_and_bounds = new decimal[n];
        cols_branch_and_bounds = new decimal[m+1];
        basis_variables_branch_and_bounds = new string[m+1];
        basis_variables_branch_and_bounds_new = new string[m+1];
        matrix_branch_and_bounds_new = new decimal[m+1,n];
        F_branch_and_bounds_new = new decimal[n];
        cols_branch_and_bounds_new = new decimal[m+1];
        basis_variables_branch_and_bounds_new_additional = new string[m+1];
        matrix_branch_and_bounds_new_additional = new decimal[m+1,n];
        F_branch_and_bounds_new_additional = new decimal[n];
        cols_branch_and_bounds_new_additional = new decimal[m+1];
    }

    // Функция печати
    public void PrintBranchAndBounds()
    {
        Console.WriteLine();
        Console.WriteLine("Получившаяся симплекс таблица:");
        Console.Write("\t" + "S_i0" + "\t");
        // for(int i = 0; i<SimplexMethod.m; ++i)
        // {
        //     Console.Write(free_variables[i] + "\t");
        // }
        // Console.WriteLine();
        for(int i = 0; i<m+1; ++i)
        {
            // Console.Write(basis_variables_branch_and_bounds_new_additional[i] + "\t");
            Console.Write($"{cols_branch_and_bounds_new_additional[i]:F2}" + "\t");
            for(int j = 0; j<n; ++j)
            {
                Console.Write($"{matrix_branch_and_bounds_new_additional[i,j]:F2}" + "\t");
            }
            Console.WriteLine("");
        }
        Console.Write("F" + "\t");
        for(int i = 0; i<n; ++i)
        {
            Console.Write($"{F_branch_and_bounds_new_additional[i]:F2}" + "\t");
        }
        Console.WriteLine($"\n\nF = {cols_branch_and_bounds_new_additional[n]:F2}");
    }

    // Функция для копирования массивов при добавлении новой переменной, которую мы оставляем пустой в данной функции
    public void CopyMassives(SimplexMethod simpobj)
    {
        if(CheckInSolve(simpobj))
        {
            int count = 1;
            Make();
            for(int i = 0; i<m; ++i)
            {
                if(i != 0)
                {
                    ++count;
                }
                basis_variables_branch_and_bounds[i] = simpobj.basis_variables[i];
                F_branch_and_bounds[i] = simpobj.cols_new[i];
                cols_branch_and_bounds[i] = simpobj.F_new[count];
                for(int j = 0; j<n; ++j)
                {
                    matrix_branch_and_bounds[j,i] = simpobj.matrix_new[i,j];
                }
            }
            cols_branch_and_bounds[simpobj.n-1] = simpobj.F_new[0];
            basis_variables_branch_and_bounds_new[simpobj.n] = $"x{simpobj.m + simpobj.n + 1}";   
        }
        else
        {
            Console.WriteLine("Целевая функция уже имеет целочисленное решение");
        }
    }

    // Функция проверки наличия оптимального решения для корня дерева
    public bool CheckInSolve(SimplexMethod simpobj)
    {
        for(int i = 0; i<m; ++i)
        {
            if(simpobj.cols_new[i] % 1 != 0)
            {
                return IsHasSolve = true;
            }
        }
        return IsHasSolve = false;
    }

    // Функция проверки наличия оптимального решения
    public bool CheckInSolve()
    {
        for(int i = 0; i<n; ++i)
        {
            if(cols_branch_and_bounds[i] % 1 != 0)
            {
                return IsHasSolve = true;
            }
        }
        return IsHasSolve = false;
    }

    // Функция проверки наличия оптимального решения для ребенка
    Node? MakeChild(Branch_and_bounds branch, Node parent, bool IsLeft)
    {
        Node? Child = null;
        if(CheckInSolve())
        {
            Child = new Node(branch);

            if(IsLeft)
            {
                CopyClass("low");
                int temp = CheckSystem();
                if(temp == 3)
                {
                    Console.WriteLine("Система не имеет решений");
                    return null;
                }
                if(temp == 5)
                {
                    for(int i = 0; i<n; ++i)
                    {
                        if(F_branch_and_bounds_new[i] > 0)
                        {
                            SimplexTab(() => Find_row(Find_cols_in_func),Find_cols_in_func);
                            Refresh_massives();
                            PrintBranchAndBounds();
                            parent.left = Child;
                        }
                    }
                }
                if(temp == 4)
                {
                    for(int i = 0; i<m+1; ++i)
                    {
                        if(cols_branch_and_bounds_new[i] < 0)
                        {
                            for(int j = 0; j<n; ++j)
                            {
                                if(matrix_branch_and_bounds_new[i,j] < 0)
                                {
                                    SimplexTab(() => Find_row(Find_col),Find_col);
                                    Refresh_massives();
                                    PrintBranchAndBounds();
                                    parent.left = Child;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                CopyClass("high");
                int temp = CheckSystem();
                if(temp == 3)
                {
                    Console.WriteLine("Система не имеет решений");
                }
                if(temp == 5)
                {
                    for(int i = 0; i<n; ++i)
                    {
                        if(F_branch_and_bounds_new[i] > 0)
                        {
                            SimplexTab(() => Find_row(Find_cols_in_func),Find_cols_in_func);
                            Refresh_massives();
                            PrintBranchAndBounds();
                            parent.right = Child;
                        }
                    }
                }
                if(temp == 4)
                {
                    for(int i = 0; i<m+1; ++i)
                    {
                        if(cols_branch_and_bounds_new[i] < 0)
                        {
                            for(int j = 0; j<n; ++j)
                            {
                                if(matrix_branch_and_bounds_new[i,j] < 0)
                                {
                                    SimplexTab(() => Find_row(Find_col),Find_col);
                                    Refresh_massives();
                                    PrintBranchAndBounds();
                                    parent.right = Child;
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Нецелого числа в столбце свободных членов нет");
        }

        return Child;
    }

    // Функция для проверки наличия решения
    int CheckSystem()
    {
        int CountVariable = 0;
        bool IfHasntASolver = true;
        for(int i = 0; i<m+1; ++i)
        {
            if(cols_branch_and_bounds_new[i] < 0)
            {
                for(int j = 0; j<n; ++j)
                {
                    if(matrix_branch_and_bounds_new[i,j] < 0)
                    {
                        IfHasntASolver = false;
                        return 4; // Найден отрицательный элемент => разрешающий столбец, находим разрешающую строчку и продолжаем
                        //симплекс-метод
                    }
                }
            }
            else
            {
                ++CountVariable; 
            }
        }
        if(CountVariable == m)
        {
            IfHasntASolver = false;
            return 5; // Опорное решение только, если все свободные кэфы >= 0
            // Опорное решение найдено, продолжаем симплекс-метод
        }
        if(IfHasntASolver)
        {
            return 3; // В строке S нет отрицательных элементов => система не имеет решений
        }

        return -1;
    }

    // Функция для копирования и заполнения симплекс таблицы и ее дополнительной переменной в зависимости от направления округления: вверх или вниз
    void CopyClass(string direction_of_roundig)
    {
        for(int i = 0; i<m+1; ++i)
        {
            basis_variables_branch_and_bounds_new[i] = basis_variables_branch_and_bounds[i];
            cols_branch_and_bounds_new[i] = cols_branch_and_bounds[i];
            for(int j = 0; j<n; ++j)
            {
                matrix_branch_and_bounds_new[i,j] = matrix_branch_and_bounds[i,j];
            }
        }
        for(int i = 0; i<n; ++i)
        {
            F_branch_and_bounds_new[i] = F_branch_and_bounds[i];
        }

        // if(IndexOfNonInteger(cols_branch_and_bounds) == -1)
        // {
        //     Console.WriteLine
        // }
        if(direction_of_roundig == "low")
        {
            int temp_variable = (int)cols_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)];
            cols_branch_and_bounds_new[m] = temp_variable - cols_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)];
            for(int i = 0; i<n; ++i)
            {
                matrix_branch_and_bounds_new[m,i] = matrix_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds),i];
            }
        }
        else if(direction_of_roundig == "high")
        {
            int temp_variable = (int)cols_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)] + 1;
            cols_branch_and_bounds_new[m] = temp_variable - cols_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)];
            for(int i = 0; i<n; ++i)
            {
                matrix_branch_and_bounds_new[m,i] = -matrix_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds),i];
            }
        }
    }

    // Функция для перемешивания массивов
    void Refresh_massives()
    {
        for(int i = 0; i<m+1; ++i)
        {
            cols_branch_and_bounds_new[i] = cols_branch_and_bounds_new_additional[i];
        }
        for(int i = 0; i<n; ++i)
        {
            F_branch_and_bounds_new[i] = F_branch_and_bounds_new_additional[i];
        }
        for(int i = 0; i<m+1; ++i)
        {
            for(int j = 0; j<n; ++j)
            {
                matrix_branch_and_bounds_new[i,j] = matrix_branch_and_bounds_new_additional[i,j];
            }
        }
    }

    // Функция для поиска нецелого числа в столбце свободных членов
    public int IndexOfNonInteger(decimal[] array)   
    {
        for(int i = 0; i<n; ++i)
        {
            if(array[i] % 1 != 0)
            {
                return i;
            }
        }
        return -1;
    }

    // Функция для поиска разрешающего столбца
    public int Find_col()
    {
        for(int i = 0; i<m+1; ++i)
        {
            if(cols_branch_and_bounds_new[i] < 0)
            {
                for(int j = 0; j<n; ++j)
                {
                    if(matrix_branch_and_bounds_new[i,j] < 0)
                    {
                        return j;
                    }
                }
            }
        }
        return -1;
    }

    // Функция для поиска разрешающей строки
    public int Find_row(Func<int> find_col)
    {
        decimal temp_var;
        decimal INFINITY = 10000000000000000000m;
        int g = -1;
        int col = find_col();
        if(col == -1)
        {
            return -1;
        }
        for(int i = 0; i<m+1; ++i)
        {
            for(int j = 0; j<n; ++j)
            {
                if(matrix_branch_and_bounds_new[i,col] != 0)
                {
                    if((cols_branch_and_bounds_new[i]/matrix_branch_and_bounds_new[i,col]) > 0)
                    {
                        temp_var = cols_branch_and_bounds_new[i]/matrix_branch_and_bounds_new[i,col];
                        if(temp_var < INFINITY)
                        {
                            g = i;
                            INFINITY = temp_var;
                        }
                    }
                }
            }
        }
        return g;
    }

    // Функция для поиска разрешающего столбца
    public int Find_cols_in_func()
    {
        for(int i = 0; i<n; ++i)
        {
            if(F_branch_and_bounds_new[i] > 0)
            {
                return i;
            }
        }
        return -1;
    }

        // Преобразование симплекс-таблицы
        void SimplexTab(Func<int> Find_rowfunc, Func<int> Find_colfunc) 
        {
            int Find_row = Find_rowfunc();
            int Find_col = Find_colfunc();
            matrix_branch_and_bounds_new_additional[Find_row,Find_col] = 1/matrix_branch_and_bounds_new[Find_row,Find_col];
            for(int i = 0; i<n; ++i)
            {
                if(i != Find_col)
                {
                    matrix_branch_and_bounds_new_additional[Find_row,i] = matrix_branch_and_bounds_new[Find_row,i]/matrix_branch_and_bounds_new[Find_row,Find_col];
                }
            }
            for(int i = 0; i<m+1; ++i)
            {
                if(i != Find_row)
                {
                    matrix_branch_and_bounds_new_additional[i,Find_col] = -matrix_branch_and_bounds_new[i,Find_col]/matrix_branch_and_bounds_new[Find_row,Find_col];
                }
            }
            for(int i = 0; i<m+1; ++i)
            {
                if(i != Find_row)
                {
                    for(int j = 0; j<n; ++j)
                    {
                        if(j != Find_col)
                        {
                            matrix_branch_and_bounds_new_additional[i,j] = matrix_branch_and_bounds_new[i,j] - (matrix_branch_and_bounds_new[Find_row,j]*matrix_branch_and_bounds_new[i,Find_col])/matrix_branch_and_bounds_new[Find_row,Find_col];
                        }
                    }
                }
            }
            cols_branch_and_bounds_new_additional[Find_row] = cols_branch_and_bounds_new[Find_row]/matrix_branch_and_bounds_new[Find_row,Find_col];
            for(int i = 0; i<m+1; ++i)
            {
                if(i != Find_row)
                {
                    cols_branch_and_bounds_new_additional[i] = cols_branch_and_bounds_new[i] - (matrix_branch_and_bounds_new[i,Find_col]*cols_branch_and_bounds_new[Find_row])/matrix_branch_and_bounds_new[Find_row,Find_col];
                }
            }
            F_branch_and_bounds_new_additional[Find_col] = -F_branch_and_bounds_new[Find_col]/matrix_branch_and_bounds_new[Find_row,Find_col];
            for(int i = 0; i<m+1; ++i)
            {
                if(i != Find_row)
                {
                    for(int j = 0; j<n; ++j)
                    {
                        if(j != Find_col)
                        {
                            F_branch_and_bounds_new_additional[j] = F_branch_and_bounds_new[j] - (matrix_branch_and_bounds_new[Find_row,j]*matrix_branch_and_bounds_new[i,Find_col])/matrix_branch_and_bounds_new[Find_row,Find_col];
                        }                        
                    }

                }

            }
        }

    // Функция для создания и возврата корня дерева
    Node ReturnParent()
    {
        Node root = new Node(this);
        return root;
    }

    // Функция для рекурсивного запуска построения потомков
    void BuildTree(Node root)
    {
        Branch_and_bounds branch = root.branch_and_bounds!;

        if(!branch.CheckInSolve())
        {
            Console.WriteLine("Найдено целочисленное решение");
            return;
        }

        Node? left_child = branch.MakeChild(branch, root, true);

        if(left_child != null)
        {
            BuildTree(left_child);
        }

        Node? right_child = branch.MakeChild(branch, root, false);

        if (right_child != null)
        {
            BuildTree(right_child);
        }
    }

    // Функция для запуска МВГ
    public void main_function(SimplexMethod simplexMethod)
    {        
        CopyMassives(simplexMethod);
        
        Node root = ReturnParent();

        Console.WriteLine("\n" + "Начинаем МВГ");

        BuildTree(root);
    }
}


class Node
{
    public Branch_and_bounds? branch_and_bounds { get; private set;}

    public Node? left { get; set; }
    public Node? right { get; set; }
    int? parents { get; set; }
    int? VariableValue { get; set; }
    string? VariableName { get; set; }
    bool? IsItNonInteger { get; set; } // Переменная для опредления наличия решения

    public Node(Branch_and_bounds? branch)
    {
        branch_and_bounds = branch;
    }
}


class Start
{
    static void Main()
    {
        Console.Write("Введите количество строк: ");
        int m = int.Parse(Console.ReadLine()!);

        Console.Write("Введите количество столбцов: ");
        int n = int.Parse(Console.ReadLine()!);

        var simplexMethod = new SimplexMethod(n, m);
        simplexMethod.main_func_for_oprimization();

        Branch_and_bounds obj1 = new Branch_and_bounds(n, m);

        obj1.main_function(simplexMethod);
    }
}

}