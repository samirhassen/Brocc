
        export interface QuestionsServerRequest
        {
            QuestionsSubmissionToken: string;
            SkipRemoveToken: boolean; //NOTE: This has no effect in production

            BankAccountNr: string;
            BankAccountNrType: string;

            Collateral: CollateralModel;

            BeneficialOwners: BeneficialOwnerModel[];

            ProductQuestions: QuestionModel[];
        }

        export interface BeneficialOwnerModel extends PersonBaseModel{
            Connection: string;
            OwnershipPercent: number | null;
        }

        export interface CollateralModel
        {
            IsApplicant: boolean | null;

            NonApplicantPerson: CollateralPersonModel;
        }

        export interface CollateralPersonModel extends PersonBaseModel{
            Email: string;

            Phone: string;
        }

        export interface PersonBaseModel
        {
            CivicNr: string;

            FirstName: string;

            LastName: string;

            AnsweredYesOnPepQuestion: boolean | null;
            PepRole: string;
            
            AnsweredYesOnIsUSPersonQuestion: boolean | null;
        }

        export interface QuestionModel
        {
            QuestionCode: string;
            AnswerCode: string;
            QuestionText: string;
            AnswerText: string;
        }