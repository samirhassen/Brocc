// Note: below object is taken directly from backend, together with comments on the fields.
export class CreateMortgageLoanStandardRequest {
    // [Required]
    public CreditNr: string;

    /// <summary>
    /// Indicates that this loan is a child loan
    /// that is notified and such as part of the main loan instead of on it's own
    /// </summary>
    public MainCreditCreditNr: string;

    /// <summary>
    /// Additional ocr that is shared with other loans and used to indicate that payments
    /// can be split between this as the client chooses.
    ///
    /// Typical use will be to combine with MainCreditCreditNr and then printing
    /// the SharedPaymentOcrNr on the notifications.
    /// </summary>
    public SharedOcrPaymentReference: string;

    /// <summary>
    /// Loan is secured by a property so it is a mortgage loan
    /// but the intent of this part of the loan is not to buy a house or
    /// do major repairs so it does not qualify for all the tax benefits of
    /// a full mortgage loan
    /// </summary>
    public IsForNonPropertyUse: boolean = true; // try set default true

    /// <summary>
    /// Something like 28 meaning the 28th of each month.
    /// </summary>
    public NotificationDueDay?: number;

    // [Required]
    public MonthlyFeeAmount: number;

    // [Required]
    public NominalInterestRatePercent: number;

    // [Required]
    public Applicants: Applicant[];

    public Documents: Document[];

    // [Required]
    public NrOfApplicants: number;

    public ProviderName: string;
    public ProviderApplicationId: string;
    public ApplicationNr: string;

    /// <summary>
    /// Capital and initial interest rate are backdated to this date
    /// to enable the first notification to include interest from before the loan was added to the system.
    /// Cannot be forward in time.
    /// If this is not included they are dated to 'today'
    /// </summary>
    public HistoricalStartDate?: Date;

    public SettlementDate: string;
    public EndDate?: string;

    /// <summary>
    /// Interest rate will be updated from reference interest and rebound at this date.
    /// If not included it will default to today.
    /// </summary>
    public NextInterestRebindDate?: string;
    public InterestRebindMounthCount?: number;

    /// <summary>
    /// Initial reference interest rate.
    ///
    /// If not included it will default to the current system value or 0 if none is present.
    /// </summary>
    public ReferenceInterestRate?: number;

    public DrawnFromLoanAmountInitialFees: AmountModel[];
    public CapitalizedInitialFees: AmountModel[];

    public Collaterals: MortgageLoanCollateralsModel;

    public ActiveDirectDebitAccount: ActiveDirectDebitAccountModel;

    /// <summary>
    /// Initial loan amount.
    /// </summary>
    public LoanAmount?: number;

    //Or LoanAmount. Cant be combined
    public LoanAmountParts: AmountModel[];

    /// <summary>
    /// Amortization chosen. Never lower than required but can be higher if the customer wants to pay faster.
    /// </summary>
    public ActualAmortizationAmount?: number;

    /// <summary>
    /// Annuities instead of fixed amortization. Can not be used together with ActualAmortizationAmount.
    /// </summary>
    public AnnuityAmount?: number;

    /// <summary>
    /// Actual amortization will be 0 until this date is passed then it will fall back to ActualAmortizationAmount
    /// </summary>
    public AmortizationExceptionUntilDate?: string;

    /// <summary>
    /// Amortization used instead of ActualAmortizationAmount during the time until exception until date
    /// </summary>
    public ExceptionAmortizationAmount?: number;

    /// <summary>
    /// Reasons for exception. Can be one of Nyproduktion, Lantbruksenhet, Sjukdom, Arbetslöshet, Dödsfall
    /// </summary>
    public AmortizationExceptionReasons: string[];

    /// <summary>
    /// When RequiredAmortizationAmount is 0 and the customer want zero we set ActualAmortizationAmount to our default minimum (which is not a regualtory requirement)
    /// and we set this date which will cause the actual amortization to be 0 until this date is passed and then it falls back to our minimum.
    /// </summary>
    public AmortizationFreeUntilDate?: string;

    /// <summary>
    /// One of the amortization codes in MortageLoanAmortizationRuleCode
    /// Inget amorteringskrav
    /// Amorteringskrav (r201616)
    /// Skärpt amorteringskrav (r201723)
    /// Alternativregeln
    /// </summary>
    public AmortizationRule: string;

    /// <summary>
    /// The loan amount ysed for amortization calculation. When moving loans will typically be from the other bank.
    /// </summary>
    public AmortizationBasisLoanAmount?: number;

    /// <summary>
    /// The object value used for amortization calculation. When moving loans will typically be from the other bank.
    /// </summary>
    public AmortizationBasisObjectValue?: number;

    /// <summary>
    /// Current estimate of the objects value
    /// </summary>
    public CurrentObjectValue?: number;

    /// <summary>
    /// The historical date when the current object value is from. If this is not supplied it's assumed to be from today.
    /// </summary>
    public CurrentObjectValueDate?: Date;

    /// <summary>
    /// The date of the object value used for amortization calculation. When moving loans will typically be from the other bank.
    /// </summary>
    public AmortizationBasisDate?: string;

    /// <summary>
    /// Used for calculating loan income ratio for r201723. This + the current loan amount will be used as the loan part of the fraction.
    /// </summary>
    public DebtIncomeRatioBasisAmount?: number;

    /// <summary>
    /// We store the current income since it's used to compute the debt income ratio for r201723
    /// </summary>
    public CurrentCombinedYearlyIncomeAmount?: number;

    /// <summary>
    /// Minimum required amortization amount when using the alternate rule. Cannot be computed since we don't know the initial loan amount
    /// and actual amortization amount can be higher. This is needed when creating an amortization basis for another bank.
    /// </summary>
    public RequiredAlternateAmortizationAmount?: number;

    public KycQuestionsJsonDocumentArchiveKey: string;
}

export class AmountModel {
    public SubAccountCode: string;
    public Amount: number;
}

class Document {
    public DocumentType: string;
    public ApplicantNr?: number;
    public ArchiveKey: string;
}

export class Applicant {
    public ApplicantNr: number;
    public CustomerId: number;
    public AgreementPdfArchiveKey: string;
    public OwnershipPercent?: number;
}

class ActiveDirectDebitAccountModel {
    public BankAccountNrOwnerApplicantNr: number;
    public BankAccountNr: string;
    public ActiveSinceDate: Date;
}

export class MortgageLoanCollateralsModel {
    public Collaterals: {
        IsMain: boolean;
        CollateralId: string;
        Properties: {
            CodeName: string;
            DisplayName: string;
            TypeCode: string;
            CodeValue: string;
            DisplayValue: string;
        }[];
        Valuations: {
            ValuationDate?: Date;
            Amount: number;
            TypeCode: string;
            SourceDescription: string;
        }[];
        CustomerIds: number[];
    }[];
}