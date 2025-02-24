namespace NTechComponents {
    export class NTechLoggingService {
        static $inject: string[] = []

        constructor() {
            this.isDebugMode = location && location.hostname === 'localhost';
        }

        public isDebugMode: boolean;
        
        public logDebug(message: string) {
            console.log(message);
        }
    }
}