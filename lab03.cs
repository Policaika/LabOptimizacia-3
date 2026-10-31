using System.Globalization;
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
        int n; // свободные переменные (столбики)
        int m; // базисные переменные ( cтроки)
        int additional_m; // дополнительная переменная
        public int int_obj;
        int low_point_of_rounding;
        int high_point_of_rounding;
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
        string[] basis_variables_after_checking = null!;
        decimal[,] matrix_new = null!; // массив с кэфами после преобразования
        decimal[,] matrix_new_branch_and_bounds = null!;
        decimal[] cols_new_add_low = null!;
        decimal[] cols_new_add_high = null!;
        decimal[] cols_new_add_main = null!;
        decimal[] round_integer_low = null!;
        decimal[] round_integer_high = null!;
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
            cols_new_add_low = new decimal[m+additional_m];
            cols_new_add_high = new decimal[m+additional_m];
            matrix_new_branch_and_bounds = new decimal[m+additional_m,n];
            basis_variables_after_checking = new string[m+additional_m];
            cols_new_add_main = new decimal[m+additional_m];
            round_integer_high = new decimal[int_obj];
            round_integer_low = new decimal[int_obj];
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

        void FindAndInitializateTheNonIntegerNum()
        {
            int_obj = 0;
            int temp_variable;
            for(int i = 0; i<m; ++i)
            {
                if(cols_new[i] % 1 != 0)
                {
                    additional_m = i+1;
                    ++int_obj;
                    temp_variable = cols_new[i];
                    low_point_of_rounding = temp_variable;
                    high_point_of_rounding = temp_variable+1;
                }
            }
        }
        // Передача всех массивов новых 

        // Функция для перемешивания массивов при добавлении новой переменной
        public void ResizeMassive(Func<int> Indexx)
        {
            int indexx = Indexx;
            MakeAdditional();
            for(int i = 0; i<m; ++i)
            {
                cols_new_add_low[i] = cols_new[i];
                cols_new_add_high[i] = cols_new[i];
                basis_variables_after_checking[i] = basis_variables[i];
                for(int j = 0; j<n; ++j)
                {
                    matrix_new_branch_and_bounds[i] = matrix_new[i];
                }
                if(i = m-1)
                {
                    cols_new_add_low[m+additional_m-1] = cols_new[indexx] - low_point_of_rounding;
                    basis_variables_after_checking[m+additional_m] = $"x{m+additional_m}";
                    for(int i = 0; i<n; ++i)
                    {
                        matrix_new_branch_and_bounds[m+additional_m,n] = -matrix_new[indexx,n];
                    }

                    cols_new_add_low[m+additional_m-1] = -cols_new[indexx] + low_point_of_rounding;
                    basis_variables_after_checking[m+additional_m] = $"x{m+additional_m}";
                    for(int i = 0; i<n; ++i)
                    {
                        matrix_new_branch_and_bounds[m+additional_m,n] = matrix_new[indexx,n];
                    }
                    // Переменная свободная в строке свободных членов
                }
            }
        }
        // ЗАМЕЧЕНИЕ: БУДУТ 3 НАБОРА МАССИВОВ: cols_new_add_high, cols_new_add_low and cols_new_add_main, в main будет класться значения массивов
        // исходим из условий ЦФ(min/max)
        // Необходимо добавить функцию, которая будет мешать и переменные сами, то есть массив с переменными свободными и базисными
        // Необходимо как-то обновлять информацию в массиве cols_new, вероятно надо переопределять его размер и закидывать в функцию MakeAdditional()
        // Необходимо сделать бесконечный цикл, пока не дойдем до всех случаев, когда у нас есть решение

        // Функция для поиска нецелого числа в столбце свободных членов
        public int IndexOfNonInteger()
        {
            for(int i = 0; i<m; ++i)
            {
                if(cols_new[i] % 1 != 0)
                {
                    return i;
                }
            }
        }

    }
}

class Branch_and_bounds
{
    // Переписать под МВГ
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


        // Главная функция, которая запускает весь процесс нахождения и оптимизации решения целевой функции
        void main_func_for_oprimization(SimplexMethod simpobj)
        {
            simpobj.Input();
            simpobj.PrintAfterGeneralSolver();
            simpobj.InputVariables();
            int check_system = simpobj.CheckSystem();
            if(check_system == 3)
            {
                Console.WriteLine("Система не имеет решений");
                Environment.Exit(0);
            }
            if(check_system == 5)
            {
                if(simpobj.Find_col_after_general_solver() == -1 || simpobj.Find_row_after_general_solver() == -1)
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
                        simpobj.SimplexTab(simpobj.Find_row_after_general_solver,simpobj.Find_col_after_general_solver);
                        simpobj.Refuel_mass();
                        simpobj.SwitchVariables(simpobj.Find_row_after_general_solver,simpobj.Find_col_after_general_solver);
                        simpobj.PrintSimplexTabWithVariables();
                    }
                }
                for(int i = 0; i<simpobj.int_obj; ++i)
                {
                    simpobj.FindAndInitializateTheNonIntegerNum();
                    simpobj.ResizeMassive();
                    
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
                        simpobj.SimplexTab(simpobj.Find_row,simpobj.Find_col);
                        simpobj.Refuel_mass();
                        simpobj.SwitchVariables(simpobj.Find_row,simpobj.Find_col);
                        simpobj.PrintSimplexTabWithVariables();
                    }
                }
                if(simpobj.Find_col_after_general_solver() == -1 || simpobj.Find_row_after_general_solver() == -1)
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
                        simpobj.SimplexTab(simpobj.Find_row_after_general_solver,simpobj.Find_col_after_general_solver);
                        simpobj.Refuel_mass();
                        simpobj.SwitchVariables(simpobj.Find_row_after_general_solver,simpobj.Find_col_after_general_solver);
                        simpobj.PrintSimplexTabWithVariables();
                    }
                }
            }
            if(Func == "max")
            {
                simpobj.ShakeFuncForPrint();
                simpobj.PrintSimplexTab();
            }
            else if(Func == "min")
            {
                simpobj.PrintSimplexTab();
            }
        }
}


class Branch_and_bounds : SimplexMethod
{
    SimplexMethod[] check_in_integer = null!;

    Branch_and_bounds()
    {
        Make();
    }

    void Make()
    {
        check_in_integer = new SimplexMethod[int_obj];
    }

    void ShakeMassWithMaidenSimpTab()
    {
        for(int i = 0; i<int_obj; ++i)
        {
            check_in_integer[i] = ResizeMassive(IndexOfNonInteger);
        }
    }
}


class Main
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