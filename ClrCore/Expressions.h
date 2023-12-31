#pragma once

#include "pch.h"

template<class T>
using wuvector = std::vector<T>;

template <class T>
using wusunique_vector = std::unique_ptr<std::vector<T>>;

template <class T>
using wusshared_vector = std::shared_ptr<std::vector<T>>;

template<class T, class U>
using wumap = std::map<T, U>;

template <class T, class U>
using wusunique_map = std::unique_ptr<std::map<T, U>>;

template <class T, class U>
using wusshared_map = std::shared_ptr<std::map<T, U>>;

template <class T>
_NODISCARD wusunique_vector<T> make_wusunique_vector() noexcept {
	return std::make_unique<std::vector<T>>();
}

template <class T>
_NODISCARD wusshared_vector<T> make_wusshared_vector() noexcept {
	return std::make_shared<std::vector<T>>();
}

template <class T, class U>
_NODISCARD wusunique_map<T, U> make_wusunique_map() noexcept {
	return std::make_unique<std::map<T, U>>();
}

template <class T, class U>
_NODISCARD wusshared_map<T, U> make_wusshared_map() noexcept {
	return std::make_shared<std::map<T, U>>();
}