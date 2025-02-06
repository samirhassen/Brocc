import { Injectable, Inject } from '@angular/core';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';
import { LOCAL_STORAGE, StorageService } from 'ngx-webstorage-service';
import { BehaviorSubject } from 'rxjs';

const LanguageStorageKey = 'balanzia.application.language.v1'
const StandardLanguage: string = 'fi'

@Injectable({
    providedIn: 'root'
})
export class LanguageService {
    currentLanguage: BehaviorSubject<string>

    constructor(private translate: TranslateService,  @Inject(LOCAL_STORAGE)private storage: StorageService) { 
        let lang: string = storage.get(LanguageStorageKey)
        if(!lang) {
            lang = translate.getBrowserLang()
        }
        if(!this.isSupportedLanguage(lang)) {
            lang = StandardLanguage
        }
        this.currentLanguage =new BehaviorSubject<string>(lang) 

        // this language will be used as a fallback when a translation isn't found in the current language
        translate.setDefaultLang(StandardLanguage)
        // the lang to use, if the lang isn't available, it will use the current loader to get them        
        translate.use(lang)
        
        translate.onLangChange.subscribe((event: LangChangeEvent) => {
            if(!this.isSupportedLanguage(event.lang)) {
                translate.use(this.currentLanguage.value)
            } else if(this.currentLanguage.value !== event.lang) {
                this.currentLanguage.next(event.lang)
            }
          })
    }

    private isSupportedLanguage(lang: string): boolean {
        return lang === 'fi' || lang === 'sv'
    }

    setLanguage(lang: string) { 
        if(!this.isSupportedLanguage(lang)) {
            return
        }
        this.storage.set(LanguageStorageKey, lang)
        this.currentLanguage.next(lang)
        this.translate.use(lang)
    }
}