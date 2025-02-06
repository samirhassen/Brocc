import { Injectable } from '@angular/core';
import { StringDictionary } from '../common.types';
import { ConfigService } from './config.service';

//TODO: Log this to somewhere serverside
@Injectable({
    providedIn: 'root',
})
export class LoggingService {
    constructor(private configService: ConfigService) {}

    private log(message: string, context: StringDictionary, level: string) {
        if (this.configService.isNTechTest()) {
            console.log(`${level}: ${message} (${JSON.stringify(context)})`);
        }
    }

    public debug(message: string, context?: StringDictionary) {
        this.log(message, context, 'debug');
    }

    public info(message: string, context?: StringDictionary) {
        this.log(message, context, 'info');
    }

    public warning(message: string, context?: StringDictionary) {
        this.log(message, context, 'warning');
    }

    public error(message: string, context?: StringDictionary) {
        this.log(message, context, 'error');
    }

    public exception(error: Error) {
        this.error(error?.message, { errorName: error?.name, errorStack: error?.stack });
    }
}
