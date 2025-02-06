import { Component, Input, OnInit, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'policyfilter-ruleset-list',
    templateUrl: './policyfilter-ruleset-list.component.html',
    styles: [],
})
export class PolicyfilterRulesetListComponent implements OnInit {
    constructor(private modalService: BsModalService, private configService: ConfigService) {}

    @Input()
    public initialData: PolicyfilterRulesetListInitialData;

    public m: Model;

    @ViewChild('changeSlotModalTemplate', { static: true })
    changeSlotModalTemplate: TemplateRef<any>;

    public changeSlotModalRef: BsModalRef;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let rs = this.initialData.ruleSets || [];

        let isEditAllowed = this.initialData.isEditAllowed;

        let pending = rs.find((x) => x.SlotName === 'Pending');
        let activeItems = rs.filter(x => !!x.SlotName && x.SlotName !== 'Pending').map(x => ({
            slotName: x.SlotName,
            ruleSetItem: x,
            functionName: isEditAllowed ? 'edit' : null
        }));
        let pendingItem = {
            slotName: 'Pending',
            ruleSetItem: pending,
            functionName: pending && isEditAllowed ? 'editAndMove' : isEditAllowed ? 'addNew' : null,
        };
        let inactiveItems = rs
            .filter((x) => !x.SlotName)
            .map((x) => {
                return {
                    slotName: '',
                    ruleSetItem: x,
                    functionName: isEditAllowed ? 'editAndMove' : null,
                };
            });
        let m: Model = {
            isAbTestingEnabled: this.initialData.isAbTestingEnabled,
            activeItems: activeItems,
            pendingItem: pendingItem,
            inactiveItems: inactiveItems,
            allItems: [...activeItems, pendingItem, ...inactiveItems],
            isWorking: false,
        };

        this.m = m;
    }

    edit(slotModel: SlotModel, evt?: Event) {
        evt?.preventDefault();

        this.m.isWorking = true;
        this.initialData.handleEdit(slotModel.ruleSetItem.Id).then((x) => {
            if (x?.showPageAfter) {
                this.m.isWorking = false;
            }
        });
    }

    getSlotDisplayName(slotName: string) {
        if (slotName == 'A') {
            return this.m.isAbTestingEnabled ? 'A' : 'Active';
        } else if (slotName) {
            return slotName;
        } else {
            return 'Inactive';
        }
    }

    changeSlot(itemToMove: SlotModel, evt?: Event) {
        evt?.preventDefault();

        let slotNames = ['A'];
        if(this.m.isAbTestingEnabled) {
            slotNames.push('B');
        }
        if(this.configService.isAnyFeatureEnabled(['ntech.feature.unsecuredloans.webapplication'])) {
            slotNames.push('WebPreScore');
        }
        slotNames.push('Pending', '')
        
        slotNames = slotNames.filter((x) => x != itemToMove.slotName);

        let possibleTargets: { slotName: string; warningText: string }[] = [];
        for (let targetSlotName of slotNames) {
            let warningText = '';
            if (
                targetSlotName &&
                this.m.allItems.find((x) => x.ruleSetItem && x.slotName && x.slotName == targetSlotName)
            ) {
                warningText = `The rule already in slot ${this.getSlotDisplayName(
                    targetSlotName
                )} will be moved to ${this.getSlotDisplayName('')}`;
            }
            possibleTargets.push({
                slotName: targetSlotName,
                warningText: warningText,
            });
        }

        this.m.changeSlot = {
            itemToMove: itemToMove,
            possibleTargets: possibleTargets,
            selectedTarget: null,
        };
        this.changeSlotModalRef = this.modalService.show(this.changeSlotModalTemplate, {
            class: 'modal-lg',
            ignoreBackdropClick: true,
        });
    }

    addNew(evt?: Event) {
        evt?.preventDefault();

        this.m.isWorking = true;
        this.initialData.handleAddNew().then((x) => {
            if (x?.showPageAfter) {
                this.m.isWorking = false;
            }
        });
    }

    move(evt?: Event) {
        evt?.preventDefault();

        let ruleSetIdToMove = this.m.changeSlot.itemToMove.ruleSetItem.Id;
        let targetSlotName = this.m.changeSlot.selectedTarget.slotName;

        this.changeSlotModalRef?.hide();
        this.m.isWorking = true;
        this.initialData.handleMove(ruleSetIdToMove, targetSlotName).then((x) => {
            if (x?.showPageAfter) {
                this.m.isWorking = false;
            }
        });
    }

    copyRuleset(rulesetItem: PolicyFilterRuleSetListItemModel, evt?: Event) {
        evt?.preventDefault();

        if (!this.m || !rulesetItem || !rulesetItem.ModelData) return;

        let copyText = `S_${btoa(rulesetItem.ModelData)}_S`;
        navigator.clipboard.writeText(copyText);
    }
}

class Model {
    isAbTestingEnabled: boolean;
    activeItems: SlotModel[]
    pendingItem: SlotModel;
    inactiveItems: SlotModel[];
    allItems: SlotModel[];
    isWorking: boolean;
    changeSlot?: {
        itemToMove: SlotModel;
        possibleTargets: { slotName: string; warningText: string }[];
        selectedTarget: { slotName: string; warningText: string };
    };
}

class SlotModel {
    slotName: string;
    ruleSetItem: PolicyFilterRuleSetListItemModel;
    functionName: string;
}

export interface PolicyFilterRuleSetListItemModel {
    //Names are capitalized to avoid remapping when sourcing this from an api call
    Id: number;
    SlotName: string;
    RuleSetName: string;
    ModelData: string;
}

export class PolicyfilterRulesetListInitialData {
    isAbTestingEnabled: boolean;
    isEditAllowed: boolean;
    ruleSets: PolicyFilterRuleSetListItemModel[];
    handleEdit: (ruleSetId: number) => Promise<{ showPageAfter: boolean }>;
    handleAddNew: () => Promise<{ showPageAfter: boolean }>;
    handleMove: (ruleSetId: number, newSlotName: string) => Promise<{ showPageAfter: boolean }>;
}
