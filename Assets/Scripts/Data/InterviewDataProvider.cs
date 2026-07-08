using System.Collections.Generic;

public static class InterviewDataProvider
{
    public static List<InterviewSection> GetSections()
    {
        return new List<InterviewSection>
        {
            new InterviewSection("General Information", new List<InterviewQuestion>
            {
                new InterviewQuestion(
                    "Where do you currently reside and work?",
                    "I live and work here in the Philippines full-time.",
                    data => data.residencyStatus = ResidencyStatus.ResidentCitizen
                ),
                new InterviewQuestion(
                    "How would you describe how you earn your income?",
                    "I have a regular job, but I also run a small online business on the side.",
                    data => data.taxpayerType = TaxpayerType.MixedIncomeEarner
                ),
                new InterviewQuestion(
                    "Can you walk me through all your sources of income this year?",
                    "I earn a salary from my employer, and additional income from my online business.",
                    data => data.incomeSource = IncomeSource.MixedIncome
                ),
            }),

            new InterviewSection("Employment", new List<InterviewQuestion>
            {
                new InterviewQuestion(
                    "How many employers did you have this year?",
                    "Just the one — I've been with the same company all year.",
                    data => data.numberOfEmployers = EmployerCount.OneEmployer
                ),
                new InterviewQuestion(
                    "Is your business formally registered with the BIR?",
                    "Yes, I registered it last year — I have the Certificate of Registration.",
                    data => data.businessRegistration = BusinessRegistration.Registered
                ),
                new InterviewQuestion(
                    "For your business income, are you using the graduated rates or the 8% option?",
                    "I opted for the 8% flat rate — it was simpler for my situation.",
                    data => data.taxOption = TaxOption.EightPercentTaxRate
                ),
            }),

            new InterviewSection("Clarifications", new List<InterviewQuestion>
            {
                new InterviewQuestion(
                    "I noticed your bank deposits seem higher than your declared sales. Can you clarify?",
                    "Some of those deposits were personal transfers from my spouse, not business income.",
                    data => { /* informational only */ }
                ),
            }),
        };
    }
}