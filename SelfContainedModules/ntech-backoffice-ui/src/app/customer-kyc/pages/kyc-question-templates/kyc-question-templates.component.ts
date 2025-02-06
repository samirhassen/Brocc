import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { EditQuestionsTemplateComponentInitialData } from '../../components/edit-questions-template/edit-questions-template.component';
import { KycTemplateService } from '../../services/kyc-template.service';

@Component({
    selector: 'app-kyc-question-templates',
    templateUrl: './kyc-question-templates.component.html',
    styles: [],
})
export class KycQuestionTemplatesComponent implements OnInit {
    constructor(private modalService: BsModalService, private apiService: KycTemplateService) {}

    public m: Model;

    @ViewChild('previewModalTemplate', { static: true })
    public previewModal: TemplateRef<any>;

    public previewModalRef: BsModalRef;

    async ngOnInit() {
        this.reload();
    }

    private async reload() {
        let initialData = await this.apiService.getAllKycQuestionTemplates();

        let m: Model = {
            isEditing: false,
            activeProducts: [],
        };

        for (let product of initialData.activeProducts) {
            m.activeProducts.push({
                relationType: product.relationType,
                currentQuestionsModelJson: product.currentQuestionsTemplate
                    ? JSON.stringify(product.currentQuestionsTemplate)
                    : null,
                historicalModels: product.historicalModels,
            });
        }

        this.m = m;
    }

    formatRelationType(relationType: string) {
        if (relationType.includes('Savings')) {
            return 'Savings account';
        } else {
            return 'Loan';
        }
    }

    beginEdit(product: ModelProduct, evt?: Event) {
        evt?.preventDefault();
        product.editData = {
            initialModelJson: product.currentQuestionsModelJson,
            onSave: async (modelJson: string) => {
                await this.apiService.setKycQuestionTemplate(product.relationType, modelJson);
                this.reload();
            },
            onCancel: async () => {
                this.m.isEditing = false;
                product.editData = null;
            },
        };
    }

    async viewHistorical(id: number, evt?: Event) {
        evt?.preventDefault();

        this.m.previewModelJson = (await this.apiService.getKycQuestionTemplateModelData(id)).modelData;

        this.previewModalRef = this.modalService.show(this.previewModal, {
            class: 'modal-xl',
            ignoreBackdropClick: true,
        });
    }

    public formatJson() {
        if (!this.m?.previewModelJson) {
            return '-';
        }
        try {
            return JSON.stringify(JSON.parse(this.m.previewModelJson), null, 2);
        } catch {
            return this.m.previewModelJson;
        }
    }
}

interface Model {
    isEditing: boolean;
    activeProducts: ModelProduct[];
    previewModelJson?: string;
}

interface ModelProduct {
    relationType: string;
    currentQuestionsModelJson: string;
    editData?: EditQuestionsTemplateComponentInitialData;
    historicalModels: {
        id: number;
        date: string;
    }[];
}
