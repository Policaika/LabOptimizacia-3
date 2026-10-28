```C#
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Threading.Channels;

namespace SimplexMethodBMSTU
{
    struct RoundNonIntegerNumber
    {
        public int high_point_of_rounding;
        public int low_point_of_rounding;
    }
    class SimplexMethod
    {
        int n; // свободные переменные (столбики)
        int m; // базисные переменные ( cтроки)
        int additional_m; // дополнительная переменная
        int int_obj;
        //bool
        // 1. Столбец S
        // 2. Массив с кэфами
        // 3. F строка(целевая функция)
        decimal[] cols = null!; // cтолбец s
        decimal[] cols_new = null!;
        decimal[,] matrix = null!; // массив с кэфами
        decimal[] F = null!;
        string Func = null!; // Определение функции (max/min)
        decimal[] F_new = null!;
        string[] basis_variables = null!;
        string[] free_variables = null!;
        RoundNonIntegerNumber[] check_in_integer = null!;
        string[] basis_variables_after_checking = null!;
        decimal[,] matrix_new = null!; // массив с кэфами после преобразования
        decimal[,] matrix_new_branch_and_bounds = null!;
        decimal[] cols_new_add_low = null!;
        decimal[] cols_new_add_high = null!;
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
        void MakeAdditional()
        {
            check_in_integer = new RoundNonIntegerNumber[int_obj];
            cols_new_add_low = new decimal[m+additional_m];
            cols_new_add_high = new decimal[m+additional_m];
            matrix_new_branch_and_bounds = new decimal[m+additional_m,n];
            basis_variables_after_checking = new string[m+additional_m];
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

        // Функция для поиска и округления нецелого значения для ЦЗЛП
        RoundNonIntegerNumber FindAndRoundTheNonIntegerNum()
        {
            int_obj = 0;
            int temp_variable;
            for(int i = 0; i<m; ++i)
            {
                if(cols_new[i] % 1 != 0)
                {
                    additional_m = m+i+1;
                    ++int_obj;
                    temp_variable = cols_new[i];
                    RoundNonIntegerNumber.low_point_of_rounding = temp_variable;
                    RoundNonIntegerNumber.high_point_of_rounding = temp_variable+1;
                }
            }
            return RoundNonIntegerNumber;
        }

        // Функция для проверки принадлежности б
        bool VariableInBasisOrNo(Func<int> Index)
        {
            int index = Index;
            for(int i = 0; i<m; ++i)
            {
                if(basis_variables[i] = $"x{Index+1}")
                {
                    return 1;
                }
            }
            return 0;
        }

        

        // Функция для перемешивания массивов при добавлении новой переменной
        void ResizeMassive(Func<int> Indexx)
        {
            int indexx = Indexx;
            MakeAdditional();
            for(int i = 0; i<m; ++i)
            {
                cols_new_add[i] = cols_new[i];
                basis_variables_after_checking[i] = basis_variables[i];
                for(int j = 0; j<n; ++j)
                {
                    matrix_new_branch_and_bounds[i] = matrix_new[i];
                }
                if(i = m-1)
                {
                    if(VariableInBasisOrNo())
                    {
                        cols_new_add_low[m+additional_m-1] = cols_new[indexx] - RoundNonIntegerNumber.low_point_of_rounding;
                        basis_variables_after_checking[m+additional_m] = $"x{m+additional_m}";
                        for(int i = 0; i<n; ++i)
                        {
                            matrix_new_branch_and_bounds[m+additional_m,n] = -matrix_new[indexx,n];
                        }
                        // Переменная свободная в строке свободных членов
                    }
                    else
                    {
                        // Переменная свободная в столбце базисных членов
                    }
                    if(VariableInBasisOrNo())
                    {
                        cols_new_add_low[m+additional_m-1] = -cols_new[indexx] + RoundNonIntegerNumber.low_point_of_rounding;
                        basis_variables_after_checking[m+additional_m] = $"x{m+additional_m}";
                        for(int i = 0; i<n; ++i)
                        {
                            matrix_new_branch_and_bounds[m+additional_m,n] = matrix_new[indexx,n];
                        }
                        // Переменная свободная в строке свободных членов
                    }
                    else
                    {
                        // Переменная свободная в столбце базисных членов
                    }
                }
            }
        }
        // ЗАМЕЧЕНИЕ: БУДУТ 3 НАБОРА МАССИВОВ: cols_new_add_high, cols_new_add_low and cols_new_add_main, в main будет класться значения массивов
        // исходим из условий ЦФ(min/max)
        // Необходимо добавить функцию, которая будет мешать и переменные сами, то есть массив с переменными свободными и базисными
        // Необходимо как-то обновлять информацию в массиве cols_new, вероятно надо переопределять его размер и закидывать в функцию MakeAdditional()
        // Дописать реализацию else в ResizeMassive
        // Необходимо сделать бесконечный цикл, пока не дойдем до всех случаев, когда у нас есть решение
        // Узнать есть ли у нас решения, когда x1<=2, матрица 2х2, то есть заняты переменные x1,x2,x3,x4, тогда x1<=2 перейдет в x1+x5=2, положим, что 
        // в столбце базисных переменных х1 нет, на ее месте стоит х4, тогда каким образом ы будем выражать эту переменную, придется использовать
        // другие переменные или как?

        // Функция для поиска нецелого числа в столбце свободных членов
        int IndexOfNonInteger()
        {
            for(int i = 0; i<m; ++i)
            {
                if(cols_new[i] % 1 != 0)
                {
                    return i;
                }
            }
        }

        // Функция для заполнения массива нецелыми переменными из получившегося оптимального решения ПЗЛП
        int CheckInInteger()
        {
            for(int i = 0; i<m; ++i)
            {
                check_in_integer[i] = FindAndRoundTheNonIntegerNum();
                if(cols_new[i] % 1 != 0)
                {
                    
                    if(check_in_integer_varialbes[i] == 
                }
                if(i=m-1)
                {
                    return 1;
                }
            }
            return 0;
        }

        // void CheckOnIntegerLinearProgramming()
        // {
        //     int temp_variable_for_rounding;
        //     for(int i = 0; i<n; i++)
        //     {
        //         temp_variable_for_rounding = check_in_integer[i];


        //     }
        // }

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
                if(CheckInInteger())
                {
                    int temp_variable_for_rounding;
                    string variable_of_rounding;
                    for(int i = 0; i<m; i++)
                    {
                        temp_variable_for_rounding = check_in_integer[i];
                        variable_of_rounding = 
                        MakeAdditional();
                        for(int )
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





}```
