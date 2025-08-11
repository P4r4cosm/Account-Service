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
    v_account_currency VARCHAR(3);
    v_opened_date TIMESTAMP WITH TIME ZONE;
    v_last_accrual_date TIMESTAMP WITH TIME ZONE;
    v_calculation_start_date TIMESTAMP WITH TIME ZONE;
    v_days_passed INT;
    DEPOSIT_ACCOUNT_TYPE INT := 1; -- 1 = Deposit
    CREDIT_TRANSACTION_TYPE INT := 0; --  0 = Credit

BEGIN
    -- 1. Получаем данные и НЕМЕДЛЕННО БЛОКИРУЕМ СТРОКУ
    SELECT
        ""Balance"",
        ""InterestRate"",
        ""Currency"",
        ""OpenedDate"",
        ""LastInterestAccrualDate""
    INTO
        v_account_balance,
        v_interest_rate,
        v_account_currency,
        v_opened_date,
        v_last_accrual_date
    FROM ""Accounts""
    WHERE ""Id"" = p_account_id
      AND ""AccountType"" = DEPOSIT_ACCOUNT_TYPE
      AND ""InterestRate"" IS NOT NULL AND ""InterestRate"" > 0
    FOR UPDATE; -- Блокировка от гонки состояний

    -- 2. Если счет найден и подходит, продолжаем
    IF FOUND THEN
        -- Используем NOW() AT TIME ZONE 'UTC' для независимости от часового пояса сервера
        v_calculation_start_date := COALESCE(v_last_accrual_date, v_opened_date);
        v_days_passed := ( (NOW() AT TIME ZONE 'UTC')::date - v_calculation_start_date::date );

        -- Начисляем проценты только если прошел хотя бы один полный день и баланс положительный
        IF v_days_passed > 0 AND v_account_balance > 0 THEN
            -- Округляем до 2 знаков после запятой
            v_interest_amount := ROUND((v_account_balance * v_interest_rate / 100 / 365) * v_days_passed, 2);

            IF v_interest_amount > 0 THEN
                -- Обновляем баланс и дату последнего начисления
                UPDATE ""Accounts""
                SET ""Balance"" = ""Balance"" + v_interest_amount,
                    ""LastInterestAccrualDate"" = NOW() AT TIME ZONE 'UTC'
                WHERE ""Id"" = p_account_id;

                -- Добавляем запись о транзакции
                INSERT INTO ""Transactions"" (""Id"", ""AccountId"", ""Amount"", ""Currency"", ""Type"", ""Description"", ""Timestamp"")
                VALUES (gen_random_uuid(),
                        p_account_id,
                        v_interest_amount,
                        v_account_currency,
                        CREDIT_TRANSACTION_TYPE,
                        'Начисление процентов по вкладу (' || v_days_passed || ' дн.)',
                        NOW() AT TIME ZONE 'UTC');
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
