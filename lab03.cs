using System.Globalization;
using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Threading.Channels;
using SimplexMethodBMSTU;

namespace SimplexMethodBMSTU
{
    class SimplexMethod
    {
        int n { get; private set; } // свободные переменные (столбики)
        int m { get; private set; } // базисные переменные ( cтроки) l;;'''''';''/> ;;;;; ;  /l90
        public int int_obj { get; private set; }
        decimal[] cols = null!; // cтолбец s
        decimal[] cols_new = null!; // cтолбец s новый
        decimal[,] matrix = null!; // массив с кэфами
        decimal[] F = null!;
        string Func = null!; // Определение функции (max/min)
        decimal[] F_new = null!;
        string[] basis_variables = null!;
        string[] free_variables = null!;
        decimal[,] matrix_new = null!; // массив с кэфами после преобразования
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
        void main_func_for_oprimization()
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
    bool IsHasSolve;
    string[] basis_variables_branch_and_bounds = null!;
    decimal[] matrix_branch_and_bounds = null!;
    decimal[] F_branch_and_bounds = null!;
    decimal[] cols_branch_and_bounds = null!;
    string[] basis_variables_branch_and_bounds_new = null!;
    decimal[] matrix_branch_and_bounds_new = null!;
    decimal[] F_branch_and_bounds_new = null!;
    decimal[] cols_branch_and_bounds_new = null!;
    string[] basis_variables_branch_and_bounds_new_additional = null!;
    decimal[] matrix_branch_and_bounds_new_additional = null!;
    decimal[] F_branch_and_bounds_new_additional = null!;
    decimal[] cols_branch_and_bounds_new_additional = null!;
    

    void Make()
    {
        matrix_branch_and_bounds = new decimal[SimplexMethod.n,SimplexMethod.m];
        F_branch_and_bounds = new decimal[SimplexMethod.m];
        cols_branch_and_bounds = new decimal[SimplexMethod.n+1];
        basis_variables_branch_and_bounds = new string[SimplexMethod.n+1];
        basis_variables_branch_and_bounds_new = new string[SimplexMethod.n+1];
        matrix_branch_and_bounds_new = new decimal[SimplexMethod.n,SimplexMethod.m];
        F_branch_and_bounds_new = new decimal[SimplexMethod.m];
        cols_branch_and_bounds_new = new decimal[SimplexMethod.n+1];
        basis_variables_branch_and_bounds_new_additional = new string[SimplexMethod.n+1];
        matrix_branch_and_bounds_new_additional = new string[SimplexMethod.n+1];
        F_branch_and_bounds_new_additional = new decimal[SimplexMethod.m];
        cols_branch_and_bounds_new_additional = new decimal[SimplexMethod.n+1];
    }

    // Функция для копирования массивов при добавлении новой переменной, которую мы оставляем пустой в данной функции
    public void CopyMassives()
    {
        int count = 1;
        Make();
        for(int i = 0; i<SimplexMethod.m; ++i)
        {
            if(i != 0)
            {
                ++count;
            }
            basis_variables_branch_and_bounds[i] = SimplexMethod.basis_variables[i];
            F_branch_and_bounds[i] = SimplexMethod.cols_new[i];
            cols_branch_and_bounds[i] = SimplexMethod.F_new[count];
            for(int j = 0; j<SimplexMethod.n; ++j)
            {
                matrix_branch_and_bounds[j,i] = SimplexMethod.matrix_new[i,j];
            }
        }
        cols_branch_and_bounds[SimplexMethod.n-1] = SimplexMethod.F_new[0];
        cols_branch_and_bounds_new[SimplexMethod.n] = $"x{SimplexMethod.m + SimplexMethod.n + 1}";
    }

    // Функция проверки наличия оптимального решения для корня дерева
    public bool CheckInSolve()
    {
        for(int i = 0; i<SimplexMethod.n; ++i)
        {
            if(SimplexMethod.cols_new[i] % 1 == 0)
            {
                return IsHasSolve = true;
            }
        }
        return IsHasSolve = false;
    }

    // Функция проверки наличия оптимального решения для ребенка
    

    Node MakeChild(Branch_and_bounds branch, Node parent, bool? IsLeft)
    {
        if(CheckInSolve())
        {
            Node Child = new Node(branch);

            if(IsLeft)
            {
                CopyClass("low");
                
                parent.left = Child;            
            }
            else
            {
                CopyClass("high");

                parent.right = Child;
            }
        }
        else
        {
            Console.WriteLine("Нецелого числа в столбце свободных членов нет");
        }

        MakeChild();
        MakeChild();

        return this;
    }

    // Функция для запуска МВГ
    void main_function(SimplexMethod simplexobject)
    {
        
    }

    // Функция для создания и возврата корня дерева(для makechild)
    Node ReturnParent()
    {
        Node root = new Node(this);
        return root;
    }

    // Функция для копирования и заполнения симплекс таблицы и ее дополнительной переменной в зависимости от направления округления: вверх или вниз
    void CopyClass(string direction_of_roundig)
    {
        for(int i = 0; i<SimplexMethod.n; ++i)
        {
            for(int i = 0; i<SimplexMethod.m; ++i)
            {
                basis_variables_branch_and_bounds_new[i] = basis_variables_branch_and_bounds[i];
                F_branch_and_bounds_new[i] = F_branch_and_bounds[i];
                cols_branch_and_bounds_new[i] = cols_branch_and_bounds[i];
                for(int j = 0; j<SimplexMethod.n; ++j)
                {
                    matrix_branch_and_bounds_new[j,i] = matrix_branch_and_bounds[j,i];
                }
            }
            cols_branch_and_bounds_new[SimplexMethod.m-1] = cols_branch_and_bounds[SimplexMethod.m-1];
        }

        if(direction_of_roundig == "low")
        {
            int temp_variable = cols_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)];
            cols_branch_and_bounds_new[SimplexMethod.n] = temp_variable - cols_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)];
            for(int i = 0; i<SimplexMethod.m; ++i)
            {
                matrix_branch_and_bounds_new[IndexOfNonInteger(cols_branch_and_bounds)] = matrix_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)];
            }
        }
        else if(direction_of_roundig == "high")
        {
            int temp_variable = cols_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)] + 1;
            cols_branch_and_bounds_new[SimplexMethod.n] = temp_variable - cols_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)];
            for(int i = 0; i<SimplexMethod.m; ++i)
            {
                matrix_branch_and_bounds_new[IndexOfNonInteger(cols_branch_and_bounds)] = -matrix_branch_and_bounds[IndexOfNonInteger(cols_branch_and_bounds)];
            }
        }
    }

    // Функция для поиска нецелого числа в столбце свободных членов
    public int IndexOfNonInteger(decimal[] array)   
    {
        for(int i = 0; i<SimplexMethod.m; ++i)
        {
            if(array[i] % 1 != 0)
            {
                return i;
            }
        }
    }

    // Функция для поиска разрешающего столбца
    public int Find_col()
    {
        for(int i = 0; i<SimplexMethod.n+1; ++i)
        {
            if(cols_branch_and_bounds_new[i] < 0)
            {
                for(int j = 0; j<SimplexMethod.m; ++j)
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
        int INFINITY = 10000000000000000000m;
        int g;
        int col = find_col;
        if(find_col == -1)
        {
            return -1;
        }
        for(int i = 0; i<SimplexMethod.n+1; ++i)
        {
            for(int j = 0; j<SimplexMethod.m; ++j)
            {
                if(matrix_branch_and_bounds_new[i,j] != 0)
                {
                    if((cols_branch_and_bounds_new[i]/matrix_branch_and_bounds_new[i,j]) > 0)
                    {
                        temp_var = cols_branch_and_bounds_new[i]/matrix_branch_and_bounds_new[i,j];
                        if(temp_var < INFINITY)
                        {
                            g = temp_var;
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
        for(int i = 0; i<SimplexMethod.m; ++i)
        {
            if(F_branch_and_bounds_new[i] > 0)
            {
                return i;
            }
        }
        return -1;
    }

        // Переписать под МВГ
        // Преобразование симплекс-таблицы
        void SimplexTab(Func<int> Find_rowfunc, Func<int> Find_colfunc) 
        {
            int Find_row = Find_rowfunc();
            int Find_col = Find_colfunc();
            matrix_branch_and_bounds_new_additional[Find_row,Find_col] = 1/matrix_branch_and_bounds_new[Find_row,Find_col];
            for(int i = 0; i<SimplexMethod.m; ++i)
            {
                if(i != Find_col)
                {
                    matrix_branch_and_bounds_new_additional[Find_row,i] = matrix_branch_and_bounds_new[Find_row,i]/matrix_branch_and_bounds_new[Find_row,Find_col];
                }
            }
            for(int i = 0; i<SimplexMethod.n+1; ++i)
            {
                if(i != Find_row)
                {
                    matrix_branch_and_bounds_new_additional[i,Find_col] = -matrix_branch_and_bounds_new[i,Find_col]/matrix_branch_and_bounds_new[Find_row,Find_col];
                }
            }
            for(int i = 0; i<SimplexMethod.n+1; ++i)
            {
                if(i != Find_row)
                {
                    for(int j = 0; j<SimplexMethod.m; ++j)
                    {
                        if(j != Find_col)
                        {
                            matrix_branch_and_bounds_new_additional[i,j] = matrix_branch_and_bounds_new[i,j] - (matrix_branch_and_bounds_new[Find_row,j]*matrix_branch_and_bounds_new[i,Find_col])/matrix_branch_and_bounds_new[Find_row,Find_col];
                        }
                    }
                }
            }
            cols_branch_and_bounds_new_additional[Find_row] = cols_branch_and_bounds_new[Find_row]/matrix_branch_and_bounds_new[Find_row,Find_col];
            for(int i = 0; i<SimplexMethod.n+1; ++i)
            {
                if(i != Find_row)
                {
                    cols_branch_and_bounds_new_additional[i] = cols_branch_and_bounds_new[i] - (matrix_branch_and_bounds_new[i,Find_col]*cols_branch_and_bounds_new[Find_row])/matrix_branch_and_bounds_new[Find_row,Find_col];
                }
            }
            F_branch_and_bounds_new_additional[Find_col1] = -F_branch_and_bounds_new[Find_col]/matrix_branch_and_bounds[Find_row,Find_col];
            for(int i = 0; i<SimplexMethod.n+1; ++i)
            {
                if(i != Find_row)
                {
                    for(int j = 0; j<SimplexMethod.m; ++j)
                    {
                        if(j != Find_col)
                        {
                            F_branch_and_bounds_new_additional[j] = F_branch_and_bounds_new[j] - (matrix_branch_and_bounds_new[Find_row,j]*matrix_branch_and_bounds_new[i,Find_col])/matrix_branch_and_bounds_new[Find_row,Find_col];
                        }                        
                    }

                }

            }
        }
}


class Node
{
    Branch_and_bounds? branch_and_bounds { get; private set;}

    int? left { get; set; }
    int? right { get; set; }
    int? parents { get; set; }
    int? VariableValue { get; set; }
    string? VariableName { get; set; }
    bool? IsItNonInteger { get; set; } // Переменная для опредления наличия решения

    Node(Branch_and_bounds? branch)
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












        }
}

}