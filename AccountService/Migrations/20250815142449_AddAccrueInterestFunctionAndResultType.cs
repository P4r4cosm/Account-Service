using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class AddAccrueInterestFunctionAndResultType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
migrationBuilder.Sql("""
        -- Шаг 1: Удаляем старый тип, если он существует, для идемпотентности.
        -- Это позволяет безопасно повторно применять миграцию, если что-то пошло не так.
        DROP TYPE IF EXISTS public.accrual_result_type;

        -- Шаг 2: Создаем наш композитный тип для возвращаемого результата.
        CREATE TYPE public.accrual_result_type AS (
            accrued_amount DECIMAL,
            period_from TIMESTAMP WITH TIME ZONE,
            period_to TIMESTAMP WITH TIME ZONE
        );
        
        -- Удаляем старую процедуру
        DROP PROCEDURE IF EXISTS accrue_interest(p_account_id UUID);
        
        -- Шаг 3: Создаем (или заменяем) саму функцию.
        -- Использование CREATE OR REPLACE FUNCTION также делает миграцию идемпотентной.
        CREATE OR REPLACE FUNCTION accrue_interest(p_account_id UUID)
        RETURNS accrual_result_type
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
           v_current_utc_time TIMESTAMP WITH TIME ZONE;
           v_days_passed INT;
           v_result accrual_result_type; 
           DEPOSIT_ACCOUNT_TYPE INT := 1;
           CREDIT_TRANSACTION_TYPE INT := 0;
        BEGIN
           v_result.accrued_amount := 0;
           v_result.period_from := NULL;
           v_result.period_to := NULL;
           
           v_current_utc_time := NOW() AT TIME ZONE 'UTC';

           SELECT "Balance", "InterestRate", "Currency", "OpenedDate", "LastInterestAccrualDate"
           INTO v_account_balance, v_interest_rate, v_account_currency, v_opened_date, v_last_accrual_date
           FROM "Accounts"
           WHERE "Id" = p_account_id
             AND "AccountType" = DEPOSIT_ACCOUNT_TYPE
             AND "InterestRate" IS NOT NULL AND "InterestRate" > 0
           FOR UPDATE;

           IF FOUND THEN
               v_calculation_start_date := COALESCE(v_last_accrual_date, v_opened_date);
               v_days_passed := (v_current_utc_time::date - v_calculation_start_date::date);

               IF v_days_passed > 0 AND v_account_balance > 0 THEN
                   v_interest_amount := ROUND((v_account_balance * v_interest_rate / 100 / 365) * v_days_passed, 2);

                   IF v_interest_amount > 0 THEN
                       UPDATE "Accounts"
                       SET "Balance" = "Balance" + v_interest_amount,
                           "LastInterestAccrualDate" = v_current_utc_time
                       WHERE "Id" = p_account_id;

                       INSERT INTO "Transactions" ("Id", "AccountId", "Amount", "Currency", "Type", "Description", "Timestamp")
                       VALUES (gen_random_uuid(), p_account_id, v_interest_amount, v_account_currency, CREDIT_TRANSACTION_TYPE,
                               'Начисление процентов по вкладу (' || v_days_passed || ' дн.)', v_current_utc_time);
                               
                       v_result.accrued_amount := v_interest_amount;
                       v_result.period_from := v_calculation_start_date;
                       v_result.period_to := v_current_utc_time;
                   END IF;
               END IF;
           END IF;
           
           RETURN v_result;
        END;
        $$;
        """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 -- 1. Удаляем функцию, которая использует тип accrual_result_type.
                                 -- Использование IF EXISTS делает откат безопасным.
                                 DROP FUNCTION IF EXISTS public.accrue_interest(UUID);

                                 -- 2. Удаляем сам тип.
                                 DROP TYPE IF EXISTS public.accrual_result_type;
                                 """);
        }
    }
}
