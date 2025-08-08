using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class AddLastInterestAccrualDateToAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE PROCEDURE accrue_interest(p_account_id UUID)
LANGUAGE plpgsql
AS $$
DECLARE
    v_interest_amount DECIMAL;
    v_account_balance DECIMAL;
    v_interest_rate DECIMAL;
    v_opened_date TIMESTAMP WITH TIME ZONE;
    v_last_accrual_date TIMESTAMP WITH TIME ZONE;
    v_calculation_start_date TIMESTAMP WITH TIME ZONE;
    v_days_passed INT;
BEGIN
    -- 1. Получаем ВСЕ необходимые данные для расчета
    SELECT
        ""Balance"",
        ""InterestRate"",
        ""OpenedDate"",
        ""LastInterestAccrualDate""
    INTO
        v_account_balance,
        v_interest_rate,
        v_opened_date,
        v_last_accrual_date
    FROM ""Accounts""
    WHERE ""Id"" = p_account_id AND ""InterestRate"" IS NOT NULL AND ""InterestRate"" > 0;

    -- 2. Если это вклад, продолжаем
    IF FOUND THEN
        -- Определяем, с какой даты начинать расчет.
        -- Если проценты еще ни разу не начислялись, начинаем с даты открытия счета.
        -- Иначе - с даты последнего начисления.
        v_calculation_start_date := COALESCE(v_last_accrual_date, v_opened_date);

        -- 3. Считаем, сколько ЦЕЛЫХ дней прошло с даты начала расчета до СЕГОДНЯ.
        -- (CURRENT_DATE - '2025-08-05'::date) даст количество дней.
        v_days_passed := (CURRENT_DATE - v_calculation_start_date::date);

        -- 4. Если прошел хотя бы один день, начисляем проценты
        IF v_days_passed > 0 THEN
            -- ФОРМУЛА: (Ежедневный процент) * (Количество прошедших дней)
            v_interest_amount := ROUND((v_account_balance * v_interest_rate / 100 / 365) * v_days_passed, 2);

            IF v_interest_amount > 0 THEN
                -- Обновляем баланс
                UPDATE ""Accounts""
                SET ""Balance"" = ""Balance"" + v_interest_amount,
                    -- 5. КРИТИЧЕСКИ ВАЖНО: Обновляем дату последнего начисления на СЕГОДНЯ!
                    ""LastInterestAccrualDate"" = NOW()
                WHERE ""Id"" = p_account_id;

                -- Добавляем транзакцию
                INSERT INTO ""Transactions"" (""Id"", ""AccountId"", ""Amount"", ""Currency"", ""Type"", ""Description"", ""Timestamp"")
                VALUES (gen_random_uuid(), p_account_id, v_interest_amount, 'RUB', 0,
                        'Начисление процентов по вкладу за ' || v_days_passed || ' дн.', NOW());
            END IF;
        END IF;
    END IF;
END;
$$;");
            
            migrationBuilder.AddColumn<DateTime>(
                name: "LastInterestAccrualDate",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS accrue_interest;");
            migrationBuilder.DropColumn(
                name: "LastInterestAccrualDate",
                table: "Accounts");
        }
    }
}
