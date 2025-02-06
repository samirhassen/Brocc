import { BehaviorSubject } from 'rxjs';

export class ProgressBarHelper {
    public current: BehaviorSubject<ProgressModel>
    public snapshot: ProgressModel

    constructor(private stepSize: number, private minValue: number, private maxValue: number, initialValue: number) {
        let iv = this.computeProgressModelFromvalue(initialValue)
        this.snapshot = iv
        this.current = new BehaviorSubject<ProgressModel>(iv)
        this.current.subscribe(x => this.snapshot = x)        
    }

    setCurrentValue(currentValue: number) {
        this.current.next(this.computeProgressModelFromvalue(currentValue))
    }

    handleClickEvent(evt: MouseEvent, nativeElement: HTMLElement) {
        let r = this.getStepValueFromEvent(evt, nativeElement)
        this.setCurrentValue(r)
    }

    addSteps(nrOfSteps: number, evt: Event) {
        if(evt) {
            evt.preventDefault()
        }
        if(!this.snapshot) {
            return
        }
        this.setCurrentValue(this.snapshot.currentValue + (nrOfSteps * this.stepSize))
    }

    private computeProgressModelFromvalue(currentValue: number): ProgressModel {
        let v = this.getSteppedValueFromValue(currentValue)
        let f = this.getFractionFromSteppedValue(v)
        return {
            currentValue: v,
            currentFraction: f,
            currentFractionPercent: Math.round(f * 100)
        }
    }
    
    private getStepValueFromEvent(evt: MouseEvent, nativeElement: HTMLElement) : number {
        let b = nativeElement.getBoundingClientRect()
        let x = evt.pageX - b.left
        let fraction =(evt.pageX - b.left) / b.width
        if(fraction < 0) {
            fraction = 0
        } else if(fraction > 1)  {
            fraction = 1
        }

        let value = Math.round(((this.maxValue - this.minValue) * fraction) + this.minValue)
        if((this.stepSize % 10) !== 0 && this.stepSize !== 1) {
            throw 'stepsize must be a multiple of 10 or be exactly 1'
        }
        let steppedValue = this.getSteppedValueFromValue(value)

        return steppedValue
    }    

    private getFractionFromSteppedValue(steppedValue: number) {
        return (steppedValue - this.minValue) / (this.maxValue - this.minValue)
    }

    private getSteppedValueFromValue(value: number) {
        if(value < this.minValue) {
            return this.minValue
        } else if(value > this.maxValue) {
            return this.maxValue
        }
        return this.stepSize === 1 ? Math.round(value) : Math.round(value / this.stepSize) * this.stepSize
    }
}

export class ProgressModel {
    public currentValue: number
    public currentFraction: number
    public currentFractionPercent: number
}