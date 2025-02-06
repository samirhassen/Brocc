import { ErrorHandler, Inject, Injectable, Injector } from '@angular/core';
import { LoggingService } from './logging.service';

@Injectable()
export class ErrorHandlerService extends ErrorHandler {
    //@ts-ignore TODO remove unused locals
    constructor(@Inject(Injector) private injector: Injector, private loggingService: LoggingService) {
        super();
    }

    handleError(error: Error) {
        this.loggingService.exception(error);
    }
}
