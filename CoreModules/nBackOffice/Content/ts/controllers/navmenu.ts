var app = angular.module('app', []);

class NavMenuCtr {
    static $inject = ['$scope', '$http', '$window', '$timeout']
    constructor(
        $scope: NavMenuNs.ILocalScope,
        private $http: ng.IHttpService,
        private $window: ng.IWindowService,
        private $timeout: ng.ITimeoutService
    ) {
        let i = (initialData as NavMenuNs.IInitialData)
        $scope.userRoles = {}
        for (let roleName of i.currentUser.menuRoles) {
            $scope.userRoles[roleName] = true
        }

        $scope.$watch('navigateToUrl', () => {
            if ($scope.navigateToUrl) {
                $scope.isLoading = true
                $timeout(() => {
                    window.location.href = $scope.navigateToUrl
                })
            }
        })

        let toItem = (groupName: string, subGroupName: string, item: NavMenuNs.IInitialDataItem): NavMenuNs.INavMenuItem => {
            let i = angular.copy(item) as NavMenuNs.INavMenuItem
            i.groupName = groupName
            i.subGroupName = subGroupName
            i.isPermitted = !i.requiredRole || $scope.userRoles[i.requiredRole]
            return i
        }
        let allItems: NavMenuNs.INavMenuItem[] = []

        const CoreGroupName : string =  'Core'

        for (let g of i.menuGroups) {
            allItems.push(..._.map(g.items, x => toItem(g.groupName, CoreGroupName, x)))

            for (let sg of g.subGroups) {
                allItems.push(..._.map(sg.items, x => toItem(g.groupName, sg.name, x)))
            }
        }

        let products: NavMenuNs.INavMenuProductModel[] = []

        let productNames = _.uniq(_.map<NavMenuNs.INavMenuItem, string>(allItems, x => x.subGroupName))

        for (let productName of productNames) {
            let productItems = _.filter<NavMenuNs.INavMenuItem>(allItems, x => x.subGroupName === productName)
            let productGroups : NavMenuNs.INavMenuGroupModel[] = []
            let tmp = _.groupBy(productItems, x => x.groupName)            
            for (const groupName in tmp) {
                let items = tmp[groupName]
                productGroups.push({ groupName: groupName, items: items, hasAnyPermittedItem: _.any(items, x => x.isPermitted) })
            }

            let productGroupGroups: NavMenuNs.INavMenuGroupGroupModel[] = []
            for (let g of _.chain(productGroups).groupBy(function (e, i) { return Math.floor(i / 3); }).toArray().value()) {
                productGroupGroups.push({ groups: g, hasAnyPermittedItem: _.any(g, x => x.hasAnyPermittedItem) })
            }

            products.push({
                className: productName === 'Core' ? null : productName.toLowerCase(),
                productName: productName,
                hasAnyPermittedItem: _.any(productGroupGroups, x => x.hasAnyPermittedItem),
                groupGroups: productGroupGroups
            })
        }

        $scope.m = {
            products: _.sortBy(products, x => i.subGroupOrder.indexOf(x.productName)),
            allowShowDetails: i.allowShowDetails
        }

        let d = document as any
        d.scope = $scope
    }
}

app.controller('ctr', NavMenuCtr)

namespace NavMenuNs {
    export interface ILocalScope extends ng.IScope {
        userRoles: { [roleName: string]: boolean }
        navigateToUrl: string
        isLoading: boolean   
        m: IModel
    }

    export interface IModel {
        allowShowDetails: boolean
        products: INavMenuProductModel[]
    }

    export interface IInitialData {
        allowShowDetails: boolean
        currentUser: IInitialDataCurrentUser
        menuGroups: IInitialDataMenuGroup[]
        subGroupOrder: string[]
    }

    export interface IInitialDataCurrentUser {
        menuRoles: string[]
    }

    export interface IInitialDataMenuGroup {
        groupName: string
        hasAnyPermittedItem: boolean
        iconUrl: string
        items: IInitialDataItem[]
        subGroups: IInitialDataMenuSubGroup[]
    }

    export interface IInitialDataMenuSubGroup {
        name: string
        items: IInitialDataItem[]
    }

    export interface IInitialDataItem {
        isPermitted: boolean
        name: string
        requiredRole: string
        url: string
    }

    export interface INavMenuItem extends IInitialDataItem {
        groupName: string
        subGroupName: string
    }

    export interface INavMenuModel {
        products: INavMenuProductModel[]
    }

    export interface INavMenuProductModel {
        className: string
        productName: string
        hasAnyPermittedItem: boolean
        groupGroups: INavMenuGroupGroupModel[]
    }

    export interface INavMenuGroupGroupModel {
        hasAnyPermittedItem: boolean
        groups: INavMenuGroupModel[]
    }

    export interface INavMenuGroupModel {
        groupName: string
        hasAnyPermittedItem: boolean
        items: INavMenuItem[]
    }
}
